using Cleaner.Cli.Rendering;
using Cleaner.Core.Abstractions;
using Cleaner.Core.Utils;
using Spectre.Console;

namespace Cleaner.Cli.Application;

public sealed partial class CleanerApp
{
    public async Task<int> ScanAsync(IReadOnlyList<ICleaner> cleaners, RunOptions options, CancellationToken cancellationToken)
    {
        var context = contextFactory.Create(options);
        var applicable = cleaners.Where(c => c.IsApplicable(context)).ToList();
        if (applicable.Count == 0)
        {
            if (options.Json)
            {
                Console.Out.WriteLine(JsonOutput.Serialize([]));
            }
            else
            {
                renderer.Line("[yellow]No applicable cleaners selected for this OS.[/]");
            }

            return 0;
        }

        if (options.Json)
        {
            // Keep stdout pure JSON: scan without any spinner/table chrome.
            var jsonRows = new List<ScanRow>(applicable.Count);
            foreach (var cleaner in applicable)
            {
                cancellationToken.ThrowIfCancellationRequested();
                jsonRows.Add(MarkCommandBased(new ScanRow(cleaner, await SafeScanAsync(cleaner, context, cancellationToken)), context));
            }

            Console.Out.WriteLine(JsonOutput.Serialize(jsonRows));
            return 0;
        }

        var rows = await renderer.ScanAsync(applicable, c => SafeScanAsync(c, context, cancellationToken), cancellationToken);
        renderer.SizeTable(MarkCommandBased(rows, context), "Reclaimable", options.Verbose);
        return 0;
    }

    public Task<int> CleanAsync(IReadOnlyList<ICleaner> cleaners, RunOptions options, CancellationToken cancellationToken) =>
        RunCleanFlowAsync(cleaners, options, cancellationToken);

    public async Task<int> InteractiveAsync(RunOptions options, CancellationToken cancellationToken)
    {
        if (!renderer.IsInteractive)
        {
            renderer.Line("[yellow]No interactive terminal detected — use 'cleaner scan' or 'cleaner clean' (with --yes) instead.[/]");
            return 1;
        }

        renderer.InteractiveHeader(updateService.CurrentVersion);

        var context = contextFactory.Create(options);
        var choosable = registry.All.Where(c => c.IsApplicable(context)).ToList();
        if (choosable.Count == 0)
        {
            renderer.Line("[yellow]No cleaners are applicable on this system.[/]");
            return 0;
        }

        // Keep the menu open after each run so finishing a clean returns to the
        // selection instead of exiting the app. Exit only when the user asks to.
        var exitCode = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            var selected = renderer.PromptSelection(choosable);
            if (selected.Count == 0)
            {
                renderer.Line("[grey]Nothing selected.[/]");
            }
            else
            {
                exitCode = await RunCleanFlowAsync(selected, options, cancellationToken);
            }

            renderer.Line(string.Empty);
            if (!renderer.Confirm("Return to the menu?", defaultValue: true))
            {
                renderer.Line("[grey]Goodbye.[/]");
                break;
            }

            renderer.Line(string.Empty);
        }

        return exitCode;
    }

    private async Task<int> RunCleanFlowAsync(IReadOnlyList<ICleaner> cleaners, RunOptions options, CancellationToken cancellationToken)
    {
        var context = contextFactory.Create(options);
        var applicable = cleaners.Where(c => c.IsApplicable(context)).ToList();
        if (applicable.Count == 0)
        {
            renderer.Line("[yellow]No applicable cleaners selected for this OS.[/]");
            return 0;
        }

        var (runnable, blocked, forceGated) = Partition(applicable, options);

        logger.Info(
            $"Clean run starting - {runnable.Count} cleaner(s): {string.Join(", ", runnable.Select(c => c.Id))}" +
            $" (dry-run: {options.DryRun}, force: {options.Force}).");

        var rows = MarkCommandBased(
            await renderer.ScanAsync(runnable, c => SafeScanAsync(c, context, cancellationToken), cancellationToken),
            context);
        renderer.SizeTable(rows, options.DryRun ? "Would free" : "Reclaimable", options.Verbose);
        ReportSkipped(blocked, forceGated);

        var total = rows.Sum(r => r.Result.TotalBytes);
        var commandBased = rows.Count(r => r.CommandBased);

        // Process-backed cleaners (e.g. docker, conda) can't be pre-measured but are still actionable
        // when their tool is present. Keep them in the run set even when the measured total is 0.
        var actionable = runnable.Where(c => c.IsAvailable(context)).ToList();
        if (total == 0 && actionable.Count == 0)
        {
            renderer.Line("[green]Nothing to reclaim — already clean.[/]");
            return 0;
        }

        if (options.DryRun)
        {
            var note = commandBased > 0
                ? $" {commandBased} command-based cleaner(s) report their size only after running."
                : string.Empty;
            renderer.Line($"[grey]Dry run — would free [bold]{SizeFormatter.Humanize(total)}[/]. Nothing was deleted.{note}[/]");
            return 0;
        }

        return await ConfirmAndCleanAsync(actionable, total, options, context, cancellationToken);
    }

    /// <summary>Partition the applicable cleaners into what can run now, what needs admin, and what needs --force.</summary>
    private (List<ICleaner> Runnable, List<ICleaner> Blocked, List<ICleaner> ForceGated) Partition(
        IReadOnlyList<ICleaner> applicable, RunOptions options)
    {
        var forceGated = applicable.Where(c => c.RequiresForce && !options.Force).ToList();
        var runnable = applicable
            .Where(c => (!c.RequiresElevation || environment.IsElevated) && !forceGated.Contains(c))
            .ToList();
        var blocked = applicable.Where(c => c.RequiresElevation && !environment.IsElevated && !forceGated.Contains(c)).ToList();
        return (runnable, blocked, forceGated);
    }

    /// <summary>Tell the user which cleaners were left out and why (needs admin / needs --force).</summary>
    private void ReportSkipped(IReadOnlyList<ICleaner> blocked, IReadOnlyList<ICleaner> forceGated)
    {
        if (blocked.Count > 0)
        {
            var names = string.Join(", ", blocked.Select(c => c.Name)).EscapeMarkup();
            renderer.Line($"[yellow]Skipped (needs admin/root): {names}. Re-run elevated to include these.[/]");
        }

        if (forceGated.Count > 0)
        {
            var names = string.Join(", ", forceGated.Select(c => c.Name)).EscapeMarkup();
            renderer.Line($"[yellow]Skipped (needs --force): {names}. These have real trade-offs — re-run with --force to include them.[/]");
        }
    }

    /// <summary>Confirm the deletion (unless --yes) and run the actionable cleaners, printing a summary.</summary>
    private async Task<int> ConfirmAndCleanAsync(
        IReadOnlyList<ICleaner> actionable,
        long total,
        RunOptions options,
        CleanupContext context,
        CancellationToken cancellationToken)
    {
        if (!options.AssumeYes && !renderer.IsInteractive)
        {
            renderer.Line("[yellow]No interactive terminal — re-run with --yes to confirm deletion.[/]");
            return 1;
        }

        var prompt = total > 0
            ? $"Delete [bold]{SizeFormatter.Humanize(total)}[/] across {actionable.Count} cleaner(s)?"
            : $"Run {actionable.Count} cleaner(s)? (size is reported after running)";
        if (!options.AssumeYes && !renderer.Confirm(prompt, defaultValue: false))
        {
            renderer.Line("[grey]Cancelled.[/]");
            return 0;
        }

        var results = await renderer.CleanAsync(actionable, c => SafeCleanAsync(c, context, cancellationToken), cancellationToken);
        renderer.CleanSummary(results);

        var freed = results.Sum(r => r.Result.BytesFreed);
        var failed = results.Count(r => r.Result.HasErrors);
        logger.Info($"Clean run finished - freed {SizeFormatter.Humanize(freed)}, {failed} cleaner(s) reported errors.");

        if (failed > 0)
        {
            renderer.Line($"[grey]Details written to the log: {logger.LogFilePath.EscapeMarkup()}[/]");
        }

        return failed > 0 ? 1 : 0;
    }
}

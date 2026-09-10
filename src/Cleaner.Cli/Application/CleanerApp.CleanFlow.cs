using Cleaner.Cli.Rendering;
using Cleaner.Core.Abstractions;
using Cleaner.Core.Utils;
using Spectre.Console;

namespace Cleaner.Cli.Application;

public sealed partial class CleanerApp
{
    /// <summary>The whole user interface: a menu looping until the user exits.</summary>
    public async Task<int> InteractiveAsync(RunOptions options, CancellationToken cancellationToken)
    {
        if (!renderer.IsInteractive)
        {
            renderer.Line("[yellow]Cleaner is interactive only and needs a real terminal.[/]");
            renderer.Line("[grey]Run it directly in a console rather than through a pipe or redirect.[/]");
            return 1;
        }

        renderer.InteractiveHeader(updateService.CurrentVersion);

        var exitCode = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            switch (renderer.PromptMainMenu())
            {
                case MainMenuChoice.Clean:
                    exitCode = await SelectAndRunAsync(options, cancellationToken);
                    break;

                case MainMenuChoice.List:
                    exitCode = List();
                    break;

                case MainMenuChoice.Update:
                    exitCode = await UpdateAsync(checkOnly: false, cancellationToken);
                    break;

                default:
                    renderer.Line("[grey]Goodbye.[/]");
                    return exitCode;
            }

            renderer.Line(string.Empty);
        }

        return exitCode;
    }

    /// <summary>Ask which cleaners to act on, then run the scan / preview / confirm / delete flow.</summary>
    private async Task<int> SelectAndRunAsync(RunOptions options, CancellationToken cancellationToken)
    {
        var context = contextFactory.Create(options);
        var choosable = registry.All.Where(c => c.IsApplicable(context)).ToList();
        if (choosable.Count == 0)
        {
            renderer.Line("[yellow]No cleaners are applicable on this system.[/]");
            return 0;
        }

        var selected = renderer.PromptSelection(choosable);
        if (selected.Count == 0)
        {
            renderer.Line("[grey]Nothing selected.[/]");
            return 0;
        }

        return await RunCleanFlowAsync(selected, context, options, cancellationToken);
    }

    /// <summary>Scans, previews, confirms, and runs the selected cleaners.</summary>
    private async Task<int> RunCleanFlowAsync(
        IReadOnlyList<ICleaner> cleaners,
        CleanupContext context,
        RunOptions options,
        CancellationToken cancellationToken)
    {
        var (runnable, blocked) = Partition(cleaners);

        logger.Info(
            $"Clean run starting - {runnable.Count} cleaner(s): {string.Join(", ", runnable.Select(c => c.Id))}.");

        var rows = MarkCommandBased(
            await renderer.ScanAsync(runnable, c => SafeScanAsync(c, context, cancellationToken), cancellationToken),
            context);
        renderer.SizeTable(rows, "Reclaimable", options.Verbose);
        ReportSkipped(blocked);

        // Command-backed cleaners may be actionable even when their size is unknown. A scan with
        // targets already proves availability and avoids walking the same directories twice.
        var scanned = rows.Where(r => r.Result.Targets.Count > 0).Select(r => r.Cleaner).ToHashSet();
        var unavailable = rows.Where(r => r.Result.ToolUnavailable).Select(r => r.Cleaner).ToHashSet();
        var available = runnable
            .Where(c => !unavailable.Contains(c) && (scanned.Contains(c) || c.IsAvailable(context)))
            .ToList();
        var scannedTotal = rows.Sum(r => r.Result.TotalBytes);
        if (scannedTotal == 0 && available.Count == 0)
        {
            renderer.Line("[green]Nothing to reclaim — already clean.[/]");
            return 0;
        }

        // Confirm warnings per cleaner before the run-wide prompt.
        var actionable = ConfirmGuarded(available);
        if (actionable.Count == 0)
        {
            renderer.Line("[grey]Nothing left to run.[/]");
            return 0;
        }

        var selectedBytes = rows
            .Where(r => actionable.Contains(r.Cleaner))
            .Sum(r => r.Result.TotalBytes);

        return await ConfirmAndCleanAsync(actionable, selectedBytes, context, cancellationToken);
    }

    /// <summary>Partition the applicable cleaners into what can run now and what needs admin.</summary>
    private (List<ICleaner> Runnable, List<ICleaner> Blocked) Partition(IReadOnlyList<ICleaner> applicable)
    {
        var runnable = applicable.Where(c => !c.RequiresElevation || environment.IsElevated).ToList();
        var blocked = applicable.Where(c => c.RequiresElevation && !environment.IsElevated).ToList();
        return (runnable, blocked);
    }

    /// <summary>Tell the user which cleaners were left out because they need admin/root.</summary>
    private void ReportSkipped(IReadOnlyList<ICleaner> blocked)
    {
        if (blocked.Count > 0)
        {
            var names = string.Join(", ", blocked.Select(c => c.Name)).EscapeMarkup();
            renderer.Line($"[yellow]Skipped (needs admin/root): {names}. Re-run elevated to include these.[/]");
        }
    }

    /// <summary>Removes cleaners whose individual confirmation warnings are declined.</summary>
    private List<ICleaner> ConfirmGuarded(IReadOnlyList<ICleaner> available)
    {
        var kept = new List<ICleaner>(available.Count);
        foreach (var cleaner in available)
        {
            if (cleaner.ConfirmationWarning is not { Length: > 0 } warning)
            {
                kept.Add(cleaner);
                continue;
            }

            var name = cleaner.Name.EscapeMarkup();
            renderer.Line($"[yellow]![/] [bold]{name}[/]: {warning.EscapeMarkup()}");
            if (renderer.Confirm($"Include [bold]{name}[/] anyway?"))
            {
                kept.Add(cleaner);
            }
            else
            {
                renderer.Line($"[grey]Skipped {name}.[/]");
            }
        }

        return kept;
    }

    /// <summary>Confirm the deletion and run the actionable cleaners, printing a summary.</summary>
    private async Task<int> ConfirmAndCleanAsync(
        IReadOnlyList<ICleaner> actionable,
        long total,
        CleanupContext context,
        CancellationToken cancellationToken)
    {
        var prompt = total > 0
            ? $"Delete [bold]{SizeFormatter.Humanize(total)}[/] across {actionable.Count} cleaner(s)?"
            : $"Run {actionable.Count} cleaner(s)? (size is reported after running)";
        if (!renderer.Confirm(prompt))
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

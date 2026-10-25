using Cleaner.Cli.Rendering;
using Cleaner.Core.Abstractions;
using Cleaner.Core.Services;
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
        using var run = logger.BeginRun();
        var (runnable, blocked) = Partition(cleaners);

        logger.Info(
            $"Clean run starting - {runnable.Count} cleaner(s): {string.Join(", ", runnable.Select(c => c.Id))}.");
        logger.Info($"Working directory: {context.WorkingDirectory}; scan roots: {string.Join(", ", context.ScanRoots)}.");
        if (blocked.Count > 0)
        {
            logger.Info($"Skipped, needs admin/root: {string.Join(", ", blocked.Select(c => c.Id))}.");
        }

        var rows = MarkCommandBased(
            await renderer.ScanAsync(runnable, c => SafeScanAsync(c, context, cancellationToken), cancellationToken),
            context);
        renderer.SizeTable(rows, "Reclaimable", options.Verbose);
        renderer.ScanNotes(rows);
        foreach (var row in rows)
        {
            foreach (var note in row.Result.Notes)
            {
                logger.Info($"[{row.Cleaner.Id}] {note}");
            }
        }

        ReportSkipped(blocked);

        // Command-backed cleaners may be actionable even when their size is unknown: an installed
        // tool still runs its own cleanup when every folder it uses is protected from direct
        // deletion. A cleaner with neither unprotected folders nor a tool sits this run out.
        var available = rows
            .Where(r => !r.Result.ToolUnavailable)
            .Where(r => r.Result.Targets.Count > 0 || r.Cleaner.IsAvailable(context))
            .Select(r => r.Cleaner)
            .ToList();
        if (rows.Sum(r => r.Result.TotalBytes) == 0 && available.Count == 0)
        {
            renderer.Line("[green]Nothing to reclaim — already clean.[/]");
            return 0;
        }

        // Confirm warnings per cleaner before the run-wide prompt.
        var actionable = ConfirmGuarded(available, context).ToHashSet();
        if (actionable.Count == 0)
        {
            renderer.Line("[grey]Nothing left to run.[/]");
            return 0;
        }

        // Needs several folders to be worth a choice, and the cleaner must be one that acts on the
        // folders it reported rather than handing the job to a tool that clears everything.
        var plannedRows = rows.Where(r => actionable.Contains(r.Cleaner)).ToList();
        var expandable = plannedRows
            .Where(r => r.Cleaner.SupportsPartialSelection && r.Result.Targets.Count > 1)
            .ToList();

        var keptPaths = expandable.Count > 0
            && renderer.Confirm("Pick individual folders before deleting? [grey](otherwise everything above is removed)[/]")
                ? renderer.PromptFolders(expandable)
                : null;

        var kept = keptPaths is null ? null : PathComparison.CreateSet(keptPaths, environment.IsLinux);
        var plan = BuildPlan(plannedRows, expandable, kept);
        if (plan.Count == 0)
        {
            renderer.Line("[grey]Nothing selected — nothing to do.[/]");
            return 0;
        }

        // Folders the user unticked are protected for every cleaner in this run, so another
        // cleaner that reported the same folder (or a parent of it) can't delete it anyway.
        var unticked = kept is null
            ? []
            : expandable.SelectMany(r => r.Result.Targets).Select(t => t.Path).Where(p => !PathComparison.IsSelected(kept, p)).ToList();
        var runContext = unticked.Count == 0 ? context : context with { Guard = context.Guard.Excluding(unticked) };

        var scope = keptPaths is null ? null : $"{keptPaths.Count} of {expandable.Sum(r => r.Result.Targets.Count)} folders";
        return await ConfirmAndCleanAsync(plan, scope, runContext, cancellationToken);
    }

    /// <summary>
    /// Each cleaner paired with exactly the targets the user reviewed: everything its scan showed, or
    /// only the folders kept in the picker. A cleaner whose folders were all unticked drops out.
    /// </summary>
    private static Dictionary<ICleaner, IReadOnlyList<CleanupTarget>> BuildPlan(
        IReadOnlyList<ScanRow> plannedRows,
        IReadOnlyList<ScanRow> offered,
        IReadOnlySet<string>? kept)
    {
        var offeredCleaners = offered.Select(r => r.Cleaner).ToHashSet();
        var plan = new Dictionary<ICleaner, IReadOnlyList<CleanupTarget>>();
        foreach (var row in plannedRows)
        {
            if (kept is null || !offeredCleaners.Contains(row.Cleaner))
            {
                plan[row.Cleaner] = row.Result.Targets;
                continue;
            }

            var approved = row.Result.Targets.Where(t => PathComparison.IsSelected(kept, t.Path)).ToList();
            if (approved.Count > 0)
            {
                plan[row.Cleaner] = approved;
            }
        }

        return plan;
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
    private List<ICleaner> ConfirmGuarded(IReadOnlyList<ICleaner> available, CleanupContext context)
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
            if (cleaner.OptIn)
            {
                renderer.Line($"  [grey]Scanned folders: {string.Join(", ", context.ScanRoots).EscapeMarkup()}[/]");
            }

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

    /// <summary>Confirm the deletion, run exactly the reviewed plan, and print and log what happened.</summary>
    private async Task<int> ConfirmAndCleanAsync(
        IReadOnlyDictionary<ICleaner, IReadOnlyList<CleanupTarget>> plan,
        string? scope,
        CleanupContext context,
        CancellationToken cancellationToken)
    {
        var total = plan.Values.Sum(targets => targets.Sum(t => t.Bytes));
        var note = scope is null ? string.Empty : $" [grey]({scope})[/]";
        var prompt = total > 0
            ? $"Delete [bold]{SizeFormatter.Humanize(total)}[/] across {plan.Count} cleaner(s){note}?"
            : $"Run {plan.Count} cleaner(s){note}? (size is reported after running)";
        if (!renderer.Confirm(prompt))
        {
            renderer.Line("[grey]Cancelled.[/]");
            logger.Info("Clean run cancelled at the confirmation prompt.");
            return 0;
        }

        LogPlan(plan);

        var before = await renderer.StatusAsync("Checking installed tools…", ct => CaptureToolHealthAsync(context, ct), cancellationToken);
        var results = await renderer.CleanAsync(
            [.. plan.Keys],
            c => SafeCleanAsync(c, context, plan[c], cancellationToken),
            cancellationToken);
        renderer.CleanSummary(results);
        var after = await renderer.StatusAsync("Re-checking installed tools…", ct => CaptureToolHealthAsync(context, ct), cancellationToken);

        var regressions = ToolHealthCheck.Regressions(before, after);
        foreach (var problem in regressions)
        {
            renderer.Line($"[red]![/] {problem.EscapeMarkup()}");
            logger.Error($"Tool check after clean: {problem}");
        }

        var failed = results.Where(r => r.Result.HasErrors).ToList();
        foreach (var row in failed)
        {
            foreach (var error in row.Result.Errors)
            {
                logger.Error($"[{row.Cleaner.Id}] {error}");
            }
        }

        logger.Info(
            $"Clean run finished - freed {SizeFormatter.Humanize(results.Sum(r => r.Result.BytesFreed))}, " +
            $"{results.Sum(r => r.Result.SkippedCount)} path(s) skipped, " +
            (failed.Count == 0 ? "no errors." : $"errors in: {string.Join(", ", failed.Select(r => r.Cleaner.Id))}."));

        var logFile = logger.RunLogFilePath ?? logger.LogFilePath;
        renderer.Line($"[grey]Every removed and skipped path is listed in {logFile.EscapeMarkup()}[/]");

        return failed.Count > 0 || regressions.Count > 0 ? 1 : 0;
    }

    /// <summary>The reviewed plan, written before anything is deleted: the record of what was approved.</summary>
    private void LogPlan(IReadOnlyDictionary<ICleaner, IReadOnlyList<CleanupTarget>> plan)
    {
        foreach (var (cleaner, targets) in plan)
        {
            if (targets.Count == 0)
            {
                logger.Detail($"[{cleaner.Id}] approved: run the tool's own cleanup");
            }

            foreach (var target in targets)
            {
                logger.Detail($"[{cleaner.Id}] approved: {target.Mode} {target.Path} ({SizeFormatter.Humanize(target.Bytes)})");
            }
        }
    }

    private static Task<ToolHealthSnapshot> CaptureToolHealthAsync(CleanupContext context, CancellationToken cancellationToken) =>
        ToolHealthCheck.CaptureAsync(context.ProcessRunner, context.FileSystem, context.Environment.PathDirectories, cancellationToken);
}

using System.Runtime.InteropServices;
using Cleaner.Cli.Rendering;
using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners;
using Cleaner.Core.Services;
using Cleaner.Core.Utils;
using Spectre.Console;

namespace Cleaner.Cli.Application;

/// <summary>
/// Orchestrates the user-facing flows (list / scan / clean / update / interactive). All rendering is
/// delegated to <see cref="IConsoleRenderer"/>; this type only decides what to do, never how to draw it.
/// </summary>
public sealed class CleanerApp(
    ICleanerRegistry registry,
    IConsoleRenderer renderer,
    IEnvironmentService environment,
    CleanupContextFactory contextFactory,
    IUpdateService updateService,
    IAppLogger logger)
{
    /// <summary>Resolve a selection from ids/category/all. Unknown ids are returned separately.</summary>
    public IReadOnlyList<ICleaner> Resolve(
        IReadOnlyList<string> ids,
        string? category,
        bool all,
        out IReadOnlyList<string> unknownIds)
    {
        if (all)
        {
            unknownIds = [];
            return registry.All;
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            unknownIds = [];
            return registry.InCategory(category);
        }

        var resolved = new List<ICleaner>();
        var unknown = new List<string>();
        foreach (var id in ids)
        {
            var cleaner = registry.Find(id);
            if (cleaner is null)
            {
                unknown.Add(id);
            }
            else
            {
                resolved.Add(cleaner);
            }
        }

        unknownIds = unknown;
        return resolved;
    }

    public int List()
    {
        var context = contextFactory.Create(new RunOptions());
        var entries = registry.All.Select(c => new CleanerListEntry(c, StatusOf(c, context))).ToList();
        renderer.CleanerList(entries, registry.Categories.Count);
        return 0;
    }

    public async Task<int> UpdateAsync(bool checkOnly, bool assumeYes, CancellationToken cancellationToken)
    {
        UpdateCheckResult check;
        try
        {
            check = await renderer.StatusAsync("Checking for updates…", updateService.CheckAsync, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            renderer.Line($"[red]Could not reach the update server:[/] {ex.Message.EscapeMarkup()}");
            return 1;
        }

        renderer.Line($"Current version: [bold]{check.CurrentVersion.EscapeMarkup()}[/]");

        if (!check.IsUpdateAvailable || check.LatestVersion is null)
        {
            var latest = check.LatestVersion ?? check.CurrentVersion;
            renderer.Line($"[green]You're on the latest version ({latest.EscapeMarkup()}).[/]");
            return 0;
        }

        renderer.Line($"Latest version:  [bold green]{check.LatestVersion.EscapeMarkup()}[/]");
        if (check.ReleaseUrl is { Length: > 0 } url)
        {
            renderer.Line($"[grey]Release notes: {url.EscapeMarkup()}[/]");
        }

        if (checkOnly)
        {
            renderer.Line("[grey]Run 'cleaner update' to install it.[/]");
            return 0;
        }

        if (check.Asset is null)
        {
            renderer.Line(
                $"[yellow]No prebuilt binary is available for this platform ({RuntimeInformation.RuntimeIdentifier.EscapeMarkup()}).[/]");
            if (check.ReleaseUrl is { Length: > 0 } releaseUrl)
            {
                renderer.Line($"[grey]Download it manually from {releaseUrl.EscapeMarkup()}[/]");
            }

            return 1;
        }

        if (!assumeYes &&
            !renderer.Confirm($"Update from [bold]{check.CurrentVersion.EscapeMarkup()}[/] to [bold green]{check.LatestVersion.EscapeMarkup()}[/]?"))
        {
            renderer.Line("[grey]Cancelled.[/]");
            return 0;
        }

        try
        {
            await renderer.DownloadAsync(
                "Downloading update",
                (progress, ct) => updateService.ApplyAsync(check, progress, ct),
                cancellationToken);
        }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or IOException)
        {
            renderer.Line($"[red]Update failed:[/] {ex.Message.EscapeMarkup()}");
            return 1;
        }

        renderer.Line($"[green]Updated to {check.LatestVersion.EscapeMarkup()}.[/] Relaunching…");
        return 0;
    }

    public async Task<int> ScanAsync(IReadOnlyList<ICleaner> cleaners, RunOptions options, CancellationToken cancellationToken)
    {
        var context = contextFactory.Create(options);
        var applicable = cleaners.Where(c => c.IsApplicable(context)).ToList();
        if (applicable.Count == 0)
        {
            renderer.Line("[yellow]No applicable cleaners selected for this OS.[/]");
            return 0;
        }

        var rows = await renderer.ScanAsync(applicable, c => SafeScanAsync(c, context, cancellationToken), cancellationToken);
        renderer.SizeTable(rows, "Reclaimable");
        return 0;
    }

    public Task<int> CleanAsync(IReadOnlyList<ICleaner> cleaners, RunOptions options, CancellationToken cancellationToken) =>
        RunCleanFlowAsync(cleaners, options, cancellationToken);

    public async Task<int> InteractiveAsync(RunOptions options, CancellationToken cancellationToken)
    {
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

        var runnable = applicable.Where(c => !c.RequiresElevation || environment.IsElevated).ToList();
        var blocked = applicable.Where(c => c.RequiresElevation && !environment.IsElevated).ToList();

        logger.Info(
            $"Clean run starting - {runnable.Count} cleaner(s): {string.Join(", ", runnable.Select(c => c.Id))}" +
            $" (dry-run: {options.DryRun}, force: {options.Force}).");

        var rows = await renderer.ScanAsync(runnable, c => SafeScanAsync(c, context, cancellationToken), cancellationToken);
        renderer.SizeTable(rows, options.DryRun ? "Would free" : "Reclaimable");

        if (blocked.Count > 0)
        {
            var names = string.Join(", ", blocked.Select(c => c.Name)).EscapeMarkup();
            renderer.Line($"[yellow]Skipped (needs admin/root): {names}. Re-run elevated to include these.[/]");
        }

        var total = rows.Sum(r => r.Result.TotalBytes);

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
            renderer.Line($"[grey]Dry run — would free [bold]{SizeFormatter.Humanize(total)}[/]. Nothing was deleted.[/]");
            return 0;
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

    private Task<ScanResult> SafeScanAsync(ICleaner cleaner, CleanupContext context, CancellationToken cancellationToken) =>
        CleanerRunner.SafeScanAsync(cleaner, context, logger, cancellationToken);

    private Task<CleanResult> SafeCleanAsync(ICleaner cleaner, CleanupContext context, CancellationToken cancellationToken) =>
        CleanerRunner.SafeCleanAsync(cleaner, context, logger, cancellationToken);

    private CleanerStatus StatusOf(ICleaner cleaner, CleanupContext context)
    {
        if (!cleaner.IsApplicable(context))
        {
            return CleanerStatus.NotApplicable;
        }

        if (cleaner.RequiresElevation && !environment.IsElevated)
        {
            return CleanerStatus.NeedsElevation;
        }

        return cleaner.IsAvailable(context) ? CleanerStatus.Available : CleanerStatus.NotFound;
    }
}

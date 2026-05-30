using System.Runtime.InteropServices;
using Cleaner.Core.Abstractions;
using Cleaner.Core.Services;
using Cleaner.Core.Utils;
using Spectre.Console;

namespace Cleaner.Cli;

/// <summary>
/// Orchestrates the user-facing flows (list / scan / clean / interactive) and owns all rendering.
/// Core stays UI-free; this is the only place that knows about Spectre.Console.
/// </summary>
public sealed class CleanerApp(
    ICleanerRegistry registry,
    IAnsiConsole console,
    IEnvironmentService environment,
    IFileSystemService fileSystem,
    IProcessRunner processRunner,
    IUpdateService updateService)
{
    private sealed record ScanRow(ICleaner Cleaner, ScanResult Result);

    private sealed record CleanRow(ICleaner Cleaner, CleanResult Result);

    public ICleanerRegistry Registry => registry;

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
        var context = CreateContext(new RunOptions());
        var table = new Table().Border(TableBorder.Rounded).Expand();
        table.AddColumn("[bold]Cleaner[/]");
        table.AddColumn("Id");
        table.AddColumn("Category");
        table.AddColumn("Status");

        string? lastCategory = null;
        foreach (var cleaner in registry.All)
        {
            if (lastCategory is not null && !string.Equals(lastCategory, cleaner.Category, StringComparison.Ordinal))
            {
                table.AddEmptyRow();
            }

            lastCategory = cleaner.Category;
            table.AddRow(
                cleaner.Name.EscapeMarkup(),
                $"[grey]{cleaner.Id.EscapeMarkup()}[/]",
                cleaner.Category.EscapeMarkup(),
                StatusMarkup(cleaner, context));
        }

        console.Write(table);
        console.MarkupLine($"[grey]{registry.All.Count} cleaners across {registry.Categories.Count} categories.[/]");
        return 0;
    }

    public async Task<int> UpdateAsync(bool checkOnly, bool assumeYes, CancellationToken cancellationToken)
    {
        UpdateCheckResult check;
        try
        {
            check = await console.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Checking for updates…", _ => updateService.CheckAsync(cancellationToken))
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            console.MarkupLine($"[red]Could not reach the update server:[/] {ex.Message.EscapeMarkup()}");
            return 1;
        }

        console.MarkupLine($"Current version: [bold]{check.CurrentVersion.EscapeMarkup()}[/]");

        if (!check.IsUpdateAvailable || check.LatestVersion is null)
        {
            var latest = check.LatestVersion ?? check.CurrentVersion;
            console.MarkupLine($"[green]You're on the latest version ({latest.EscapeMarkup()}).[/]");
            return 0;
        }

        console.MarkupLine($"Latest version:  [bold green]{check.LatestVersion.EscapeMarkup()}[/]");
        if (check.ReleaseUrl is { Length: > 0 } url)
        {
            console.MarkupLine($"[grey]Release notes: {url.EscapeMarkup()}[/]");
        }

        if (checkOnly)
        {
            console.MarkupLine("[grey]Run 'cleaner update' to install it.[/]");
            return 0;
        }

        if (check.Asset is null)
        {
            console.MarkupLine(
                $"[yellow]No prebuilt binary is available for this platform ({RuntimeInformation.RuntimeIdentifier.EscapeMarkup()}).[/]");
            if (check.ReleaseUrl is { Length: > 0 } releaseUrl)
            {
                console.MarkupLine($"[grey]Download it manually from {releaseUrl.EscapeMarkup()}[/]");
            }

            return 1;
        }

        if (!assumeYes &&
            !console.Confirm($"Update from [bold]{check.CurrentVersion.EscapeMarkup()}[/] to [bold green]{check.LatestVersion.EscapeMarkup()}[/]?"))
        {
            console.MarkupLine("[grey]Cancelled.[/]");
            return 0;
        }

        try
        {
            await console.Progress()
                .Columns(
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn())
                .StartAsync(async ctx =>
                {
                    var task = ctx.AddTask("Downloading update", maxValue: 1.0);
                    var progress = new Progress<double>(value => task.Value = value);
                    await updateService.ApplyAsync(check, progress, cancellationToken).ConfigureAwait(false);
                    task.Value = 1.0;
                }).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or IOException)
        {
            console.MarkupLine($"[red]Update failed:[/] {ex.Message.EscapeMarkup()}");
            return 1;
        }

        console.MarkupLine($"[green]Updated to {check.LatestVersion.EscapeMarkup()}.[/] Relaunching…");
        return 0;
    }

    public async Task<int> ScanAsync(IReadOnlyList<ICleaner> cleaners, RunOptions options, CancellationToken cancellationToken)
    {
        var context = CreateContext(options);
        var applicable = cleaners.Where(c => c.IsApplicable(context)).ToList();
        if (applicable.Count == 0)
        {
            console.MarkupLine("[yellow]No applicable cleaners selected for this OS.[/]");
            return 0;
        }

        var rows = await CollectScansAsync(applicable, context, cancellationToken).ConfigureAwait(false);
        RenderSizeTable(rows, "Reclaimable");
        return 0;
    }

    public Task<int> CleanAsync(IReadOnlyList<ICleaner> cleaners, RunOptions options, CancellationToken cancellationToken) =>
        RunCleanFlowAsync(cleaners, options, cancellationToken);

    public async Task<int> InteractiveAsync(RunOptions options, CancellationToken cancellationToken)
    {
        console.Write(new FigletText("Cleaner").Color(Color.Teal));
        console.MarkupLine("[grey]Reclaim disk space from dev, OS, and app caches.[/]");
        console.WriteLine();

        var context = CreateContext(options);
        var choosable = registry.All.Where(c => c.IsApplicable(context)).ToList();
        if (choosable.Count == 0)
        {
            console.MarkupLine("[yellow]No cleaners are applicable on this system.[/]");
            return 0;
        }

        var prompt = new MultiSelectionPrompt<string>()
            .Title("Select what to [green]clean[/]:")
            .PageSize(20)
            .MoreChoicesText("[grey](move up/down to reveal more)[/]")
            .InstructionsText("[grey](space to toggle, enter to confirm)[/]");

        var labels = new Dictionary<string, ICleaner>(StringComparer.Ordinal);
        foreach (var category in choosable.Select(c => c.Category).Distinct(StringComparer.Ordinal))
        {
            var items = new List<string>();
            foreach (var cleaner in choosable.Where(c => string.Equals(c.Category, category, StringComparison.Ordinal)))
            {
                var label = Label(cleaner);
                labels[label] = cleaner;
                items.Add(label);
            }

            prompt.AddChoiceGroup(category, items);
        }

        var picked = console.Prompt(prompt);
        if (picked.Count == 0)
        {
            console.MarkupLine("[grey]Nothing selected.[/]");
            return 0;
        }

        var selected = picked.Select(p => labels[p]).ToList();
        return await RunCleanFlowAsync(selected, options, cancellationToken).ConfigureAwait(false);
    }

    private async Task<int> RunCleanFlowAsync(IReadOnlyList<ICleaner> cleaners, RunOptions options, CancellationToken cancellationToken)
    {
        var context = CreateContext(options);
        var applicable = cleaners.Where(c => c.IsApplicable(context)).ToList();
        if (applicable.Count == 0)
        {
            console.MarkupLine("[yellow]No applicable cleaners selected for this OS.[/]");
            return 0;
        }

        var runnable = applicable.Where(c => !c.RequiresElevation || environment.IsElevated).ToList();
        var blocked = applicable.Where(c => c.RequiresElevation && !environment.IsElevated).ToList();

        var rows = await CollectScansAsync(runnable, context, cancellationToken).ConfigureAwait(false);
        RenderSizeTable(rows, options.DryRun ? "Would free" : "Reclaimable");

        if (blocked.Count > 0)
        {
            var names = string.Join(", ", blocked.Select(c => c.Name)).EscapeMarkup();
            console.MarkupLine($"[yellow]Skipped (needs admin/root): {names}. Re-run elevated to include these.[/]");
        }

        var total = rows.Sum(r => r.Result.TotalBytes);

        // Process-backed cleaners (e.g. docker, conda) can't be pre-measured but are still actionable
        // when their tool is present. Keep them in the run set even when the measured total is 0.
        var actionable = runnable.Where(c => c.IsAvailable(context)).ToList();
        if (total == 0 && actionable.Count == 0)
        {
            console.MarkupLine("[green]Nothing to reclaim — already clean.[/]");
            return 0;
        }

        if (options.DryRun)
        {
            console.MarkupLine($"[grey]Dry run — would free [bold]{SizeFormatter.Humanize(total)}[/]. Nothing was deleted.[/]");
            return 0;
        }

        var prompt = total > 0
            ? $"Delete [bold]{SizeFormatter.Humanize(total)}[/] across {actionable.Count} cleaner(s)?"
            : $"Run {actionable.Count} cleaner(s)? (size is reported after running)";
        if (!options.AssumeYes && !console.Confirm(prompt, defaultValue: false))
        {
            console.MarkupLine("[grey]Cancelled.[/]");
            return 0;
        }

        var results = await RunCleansAsync(actionable, context, cancellationToken).ConfigureAwait(false);
        RenderCleanSummary(results);
        return results.Any(r => r.Result.HasErrors) ? 1 : 0;
    }

    private async Task<List<ScanRow>> CollectScansAsync(IReadOnlyList<ICleaner> cleaners, CleanupContext context, CancellationToken cancellationToken)
    {
        var rows = new List<ScanRow>(cleaners.Count);
        await console.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Scanning…", async ctx =>
            {
                foreach (var cleaner in cleaners)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    ctx.Status($"Scanning [green]{cleaner.Name.EscapeMarkup()}[/]…");
                    var result = await cleaner.ScanAsync(context, cancellationToken).ConfigureAwait(false);
                    rows.Add(new ScanRow(cleaner, result));
                }
            }).ConfigureAwait(false);

        return rows;
    }

    private async Task<List<CleanRow>> RunCleansAsync(IReadOnlyList<ICleaner> cleaners, CleanupContext context, CancellationToken cancellationToken)
    {
        var results = new List<CleanRow>(cleaners.Count);
        await console.Progress()
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("Cleaning", maxValue: cleaners.Count);
                foreach (var cleaner in cleaners)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    task.Description = $"Cleaning [green]{cleaner.Name.EscapeMarkup()}[/]";
                    var result = await cleaner.CleanAsync(context, progress: null, cancellationToken).ConfigureAwait(false);
                    results.Add(new CleanRow(cleaner, result));
                    task.Increment(1);
                }

                task.Description = "Done";
            }).ConfigureAwait(false);

        return results;
    }

    private void RenderSizeTable(IReadOnlyList<ScanRow> rows, string sizeHeader)
    {
        var nonEmpty = rows.Where(r => r.Result.TotalBytes > 0).OrderByDescending(r => r.Result.TotalBytes).ToList();
        if (nonEmpty.Count == 0)
        {
            console.MarkupLine("[green]Nothing found to clean.[/]");
            return;
        }

        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("[bold]Cleaner[/]");
        table.AddColumn(new TableColumn($"[bold]{sizeHeader.EscapeMarkup()}[/]").RightAligned());

        foreach (var row in nonEmpty)
        {
            table.AddRow(
                row.Cleaner.Name.EscapeMarkup(),
                $"[yellow]{SizeFormatter.Humanize(row.Result.TotalBytes)}[/]");
        }

        var total = rows.Sum(r => r.Result.TotalBytes);
        table.AddEmptyRow();
        table.AddRow("[bold]Total[/]", $"[bold yellow]{SizeFormatter.Humanize(total)}[/]");
        console.Write(table);
    }

    private void RenderCleanSummary(IReadOnlyList<CleanRow> results)
    {
        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("[bold]Cleaner[/]");
        table.AddColumn(new TableColumn("[bold]Freed[/]").RightAligned());
        table.AddColumn("[bold]Result[/]");

        foreach (var row in results.OrderByDescending(r => r.Result.BytesFreed))
        {
            var status = row.Result.HasErrors
                ? $"[red]{row.Result.Errors.Count} error(s)[/]"
                : "[green]ok[/]";
            table.AddRow(
                row.Cleaner.Name.EscapeMarkup(),
                $"[green]{SizeFormatter.Humanize(row.Result.BytesFreed)}[/]",
                status);
        }

        var total = results.Sum(r => r.Result.BytesFreed);
        table.AddEmptyRow();
        table.AddRow("[bold]Total[/]", $"[bold green]{SizeFormatter.Humanize(total)}[/]", string.Empty);
        console.Write(table);

        foreach (var row in results.Where(r => r.Result.HasErrors))
        {
            foreach (var error in row.Result.Errors)
            {
                console.MarkupLine($"[red]![/] [grey]{row.Cleaner.Id.EscapeMarkup()}:[/] {error.EscapeMarkup()}");
            }
        }
    }

    private string StatusMarkup(ICleaner cleaner, CleanupContext context)
    {
        if (!cleaner.IsApplicable(context))
        {
            return "[grey]n/a (other OS)[/]";
        }

        if (cleaner.RequiresElevation && !environment.IsElevated)
        {
            return "[yellow]needs admin[/]";
        }

        return cleaner.IsAvailable(context) ? "[green]available[/]" : "[grey]not found[/]";
    }

    private static string Label(ICleaner cleaner) => cleaner.Name;

    private CleanupContext CreateContext(RunOptions options) => new()
    {
        FileSystem = fileSystem,
        Environment = environment,
        ProcessRunner = processRunner,
        DryRun = options.DryRun,
        Force = options.Force,
        WorkingDirectory = options.WorkingDirectory,
    };
}

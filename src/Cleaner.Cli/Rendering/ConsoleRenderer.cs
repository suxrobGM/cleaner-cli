using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners;
using Cleaner.Core.Utils;
using Spectre.Console;

namespace Cleaner.Cli.Rendering;

/// <summary>The Spectre.Console implementation of <see cref="IConsoleRenderer"/>.</summary>
public sealed class ConsoleRenderer(IAnsiConsole console) : IConsoleRenderer
{
    public bool IsInteractive => console.Profile.Capabilities.Interactive;

    public void Line(string markup) => console.MarkupLine(markup);

    public void InteractiveHeader(string version)
    {
        console.Write(new FigletText("Cleaner").Color(Color.Teal));
        console.MarkupLine($"[grey]Reclaim disk space from dev, OS, and app caches.[/] [dim]v{version.EscapeMarkup()}[/]");
        console.WriteLine();
    }

    public void CleanerList(IReadOnlyList<CleanerListEntry> entries)
    {
        // One table per group, categories as section rows inside it. With a hundred-odd cleaners a
        // flat table is a wall of text; the grouping is what makes it scannable.
        var categories = 0;
        var groups = 0;
        foreach (var group in ByLayout(entries, e => e.Cleaner.Category))
        {
            groups++;
            var table = new Table()
                .Border(TableBorder.Rounded)
                .Expand()
                .Title($"[bold teal]{group.Key.EscapeMarkup()}[/]");
            table.AddColumn("[bold]Cleaner[/]");
            table.AddColumn("Id");
            table.AddColumn("Status");

            var first = true;
            foreach (var category in group)
            {
                categories++;
                if (!first)
                {
                    table.AddEmptyRow();
                }

                first = false;
                table.AddRow($"[bold]{category.Key.EscapeMarkup()}[/]", string.Empty, string.Empty);

                foreach (var entry in category)
                {
                    table.AddRow(
                        $"  {entry.Cleaner.Name.EscapeMarkup()}",
                        $"[grey]{entry.Cleaner.Id.EscapeMarkup()}[/]",
                        StatusMarkup(entry.Status));
                }
            }

            console.Write(table);
        }

        console.MarkupLine($"[grey]{entries.Count} cleaners across {categories} categories in {groups} groups.[/]");
    }

    public void Pause(string markup)
    {
        if (!IsInteractive)
        {
            return;
        }

        console.MarkupLine(markup);
        console.Input.ReadKey(intercept: true);
    }

    public void SizeTable(IReadOnlyList<ScanRow> rows, string sizeHeader, bool verbose = false)
    {
        // Cleaners that could not measure themselves keep their row, with a label instead of a
        // misleading 0 B. Sorting by size puts them last naturally.
        var visible = rows
            .Where(r => r.Result.TotalBytes > 0 || r.CommandBased)
            .OrderByDescending(r => r.Result.TotalBytes)
            .ToList();
        if (visible.Count == 0)
        {
            console.MarkupLine("[green]Nothing found to clean.[/]");
            return;
        }

        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("[bold]Cleaner[/]");
        table.AddColumn(new TableColumn($"[bold]{sizeHeader.EscapeMarkup()}[/]").RightAligned());

        foreach (var row in visible)
        {
            var size = row.Result.TotalBytes > 0
                ? $"[yellow]{SizeFormatter.Humanize(row.Result.TotalBytes)}[/]"
                : "[grey]n/a (unknown until it runs)[/]";
            table.AddRow(row.Cleaner.Name.EscapeMarkup(), size);

            if (verbose)
            {
                AppendVerboseTargets(table, row);
            }
        }

        var total = rows.Sum(r => r.Result.TotalBytes);
        table.AddEmptyRow();
        table.AddRow("[bold]Total[/]", $"[bold yellow]{SizeFormatter.Humanize(total)}[/]");
        console.Write(table);
    }

    public void CleanSummary(IReadOnlyList<CleanRow> results)
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

    public bool Confirm(string markup, bool defaultValue = false) =>
        IsInteractive ? console.Confirm(markup, defaultValue) : defaultValue;

    public MainMenuChoice PromptMainMenu()
    {
        // Spectre's SelectionPrompt is string-based, so keep the label/choice mapping in one place
        // rather than parsing the returned text back into an enum.
        var choices = new (string Label, MainMenuChoice Choice)[]
        {
            ("Clean caches", MainMenuChoice.Clean),
            ("Preview only (nothing is deleted)", MainMenuChoice.Preview),
            ("List all cleaners", MainMenuChoice.List),
            ("Check for updates", MainMenuChoice.Update),
            ("Exit", MainMenuChoice.Exit),
        };

        var prompt = new SelectionPrompt<string>()
            .Title("What would you like to do?")
            .AddChoices(choices.Select(c => c.Label));

        var picked = console.Prompt(prompt);
        return choices.First(c => string.Equals(c.Label, picked, StringComparison.Ordinal)).Choice;
    }

    public IReadOnlyList<ICleaner> PromptSelection(IReadOnlyList<ICleaner> choosable)
    {
        var prompt = new MultiSelectionPrompt<string>()
            .Title("Select what to [green]clean[/]:")
            .PageSize(20)
            .MoreChoicesText("[grey](move up/down to reveal more)[/]")
            .InstructionsText("[grey](space to toggle, enter to confirm — toggle a group or [bold]All cleaners[/] to take everything under it)[/]");

        // Nest categories under their group, and every group under a single "All cleaners" node. In
        // the default Leaf selection mode, toggling a parent cascades to its descendants while only
        // leaf cleaners are returned, so each tier is a one-keystroke bulk select for free.
        // Labels embed the unique Id so two cleaners sharing a display name can never mismap.
        var labels = new Dictionary<string, ICleaner>(StringComparer.Ordinal);
        var all = prompt.AddChoice("All cleaners");

        foreach (var group in ByLayout(choosable, c => c.Category))
        {
            var groupNode = all.AddChild($"[bold]{group.Key}[/]");
            foreach (var category in group)
            {
                var categoryNode = groupNode.AddChild(category.Key);
                foreach (var cleaner in category)
                {
                    var label = $"{cleaner.Name} [grey]({cleaner.Id})[/]";
                    labels[label] = cleaner;
                    categoryNode.AddChild(label);
                }
            }
        }

        var picked = console.Prompt(prompt);
        return picked.Where(labels.ContainsKey).Select(p => labels[p]).ToList();
    }

    /// <summary>
    /// Items nested group over category, both in display order. The list and the selection prompt
    /// walk the same shape, so the layout is expressed once.
    /// </summary>
    private static IEnumerable<IGrouping<string, IGrouping<string, T>>> ByLayout<T>(
        IEnumerable<T> items,
        Func<T, string> categoryOf) =>
        items.GroupBy(categoryOf, StringComparer.Ordinal)
            .OrderBy(c => Categories.RankOf(c.Key))
            .GroupBy(c => Categories.GroupOf(c.Key), StringComparer.Ordinal)
            .OrderBy(g => Categories.RankOfGroup(g.Key));

    public Task<T> StatusAsync<T>(string status, Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken) =>
        console.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync(status, _ => work(cancellationToken));

    public async Task<IReadOnlyList<ScanRow>> ScanAsync(
        IReadOnlyList<ICleaner> cleaners,
        Func<ICleaner, Task<ScanResult>> scan,
        CancellationToken cancellationToken)
    {
        // Scans are disk-walk heavy and independent, so run them concurrently; the array keeps the
        // caller's ordering stable regardless of completion order.
        var rows = new ScanRow[cleaners.Count];
        var scanned = 0;
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Clamp(Environment.ProcessorCount, 1, 8),
            CancellationToken = cancellationToken,
        };

        Task ScanAllAsync(Action<int>? onScanned) =>
            Parallel.ForEachAsync(Enumerable.Range(0, cleaners.Count), parallelOptions, async (i, _) =>
            {
                rows[i] = new ScanRow(cleaners[i], await scan(cleaners[i]));
                onScanned?.Invoke(Interlocked.Increment(ref scanned));
            });

        await console.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Scanning… [green]0/{cleaners.Count}[/]", ctx =>
                ScanAllAsync(done => ctx.Status($"Scanning… [green]{done}/{cleaners.Count}[/]")));

        return rows;
    }

    public async Task<IReadOnlyList<CleanRow>> CleanAsync(
        IReadOnlyList<ICleaner> cleaners,
        Func<ICleaner, Task<CleanResult>> clean,
        CancellationToken cancellationToken)
    {
        var results = new List<CleanRow>(cleaners.Count);
        await BarProgress()
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("Cleaning", maxValue: cleaners.Count);
                foreach (var cleaner in cleaners)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    task.Description = $"Cleaning [green]{cleaner.Name.EscapeMarkup()}[/]";
                    results.Add(new CleanRow(cleaner, await clean(cleaner)));
                    task.Increment(1);
                }

                task.Description = "Done";
            });

        return results;
    }

    public Task DownloadAsync(
        string description,
        Func<IProgress<double>, CancellationToken, Task> work,
        CancellationToken cancellationToken) =>
        BarProgress()
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask(description, maxValue: 1.0);
                var progress = new Progress<double>(value => task.Value = value);
                await work(progress, cancellationToken);
                task.Value = 1.0;
            });

    /// <summary>A progress display with the standard description / bar / percentage / spinner columns.</summary>
    private Progress BarProgress() =>
        console.Progress()
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn());

    /// <summary>Indented per-target rows under a cleaner, largest first, shown in verbose mode.</summary>
    private static void AppendVerboseTargets(Table table, ScanRow row)
    {
        foreach (var target in row.Result.Targets.Where(t => t.Bytes > 0).OrderByDescending(t => t.Bytes))
        {
            table.AddRow(
                $"  [grey]{target.Path.EscapeMarkup()}[/]",
                $"[grey]{SizeFormatter.Humanize(target.Bytes)}[/]");
        }
    }

    private static string StatusMarkup(CleanerStatus status) => status switch
    {
        CleanerStatus.NotApplicable => "[grey]n/a (other OS)[/]",
        CleanerStatus.NeedsElevation => "[yellow]needs admin[/]",
        CleanerStatus.Available => "[green]available[/]",
        CleanerStatus.NotFound => "[grey]not found[/]",
        _ => string.Empty,
    };
}

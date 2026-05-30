using Cleaner.Core.Abstractions;
using Cleaner.Core.Utils;
using Spectre.Console;

namespace Cleaner.Cli.Rendering;

/// <summary>The Spectre.Console implementation of <see cref="IConsoleRenderer"/>.</summary>
public sealed class ConsoleRenderer(IAnsiConsole console) : IConsoleRenderer
{
    public void Line(string markup) => console.MarkupLine(markup);

    public void InteractiveHeader()
    {
        console.Write(new FigletText("Cleaner").Color(Color.Teal));
        console.MarkupLine("[grey]Reclaim disk space from dev, OS, and app caches.[/]");
        console.WriteLine();
    }

    public void CleanerList(IReadOnlyList<CleanerListEntry> entries, int categoryCount)
    {
        var table = new Table().Border(TableBorder.Rounded).Expand();
        table.AddColumn("[bold]Cleaner[/]");
        table.AddColumn("Id");
        table.AddColumn("Category");
        table.AddColumn("Status");

        string? lastCategory = null;
        foreach (var entry in entries)
        {
            var cleaner = entry.Cleaner;
            if (lastCategory is not null && !string.Equals(lastCategory, cleaner.Category, StringComparison.Ordinal))
            {
                table.AddEmptyRow();
            }

            lastCategory = cleaner.Category;
            table.AddRow(
                cleaner.Name.EscapeMarkup(),
                $"[grey]{cleaner.Id.EscapeMarkup()}[/]",
                cleaner.Category.EscapeMarkup(),
                StatusMarkup(entry.Status));
        }

        console.Write(table);
        console.MarkupLine($"[grey]{entries.Count} cleaners across {categoryCount} categories.[/]");
    }

    public void SizeTable(IReadOnlyList<ScanRow> rows, string sizeHeader)
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

    public bool Confirm(string markup, bool defaultValue = false) => console.Confirm(markup, defaultValue);

    public IReadOnlyList<ICleaner> PromptSelection(IReadOnlyList<ICleaner> choosable)
    {
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
                labels[cleaner.Name] = cleaner;
                items.Add(cleaner.Name);
            }

            prompt.AddChoiceGroup(category, items);
        }

        var picked = console.Prompt(prompt);
        return picked.Select(p => labels[p]).ToList();
    }

    public Task<T> StatusAsync<T>(string status, Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken) =>
        console.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync(status, _ => work(cancellationToken));

    public async Task<IReadOnlyList<ScanRow>> ScanAsync(
        IReadOnlyList<ICleaner> cleaners,
        Func<ICleaner, Task<ScanResult>> scan,
        CancellationToken cancellationToken)
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
                    rows.Add(new ScanRow(cleaner, await scan(cleaner).ConfigureAwait(false)));
                }
            }).ConfigureAwait(false);

        return rows;
    }

    public async Task<IReadOnlyList<CleanRow>> CleanAsync(
        IReadOnlyList<ICleaner> cleaners,
        Func<ICleaner, Task<CleanResult>> clean,
        CancellationToken cancellationToken)
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
                    results.Add(new CleanRow(cleaner, await clean(cleaner).ConfigureAwait(false)));
                    task.Increment(1);
                }

                task.Description = "Done";
            }).ConfigureAwait(false);

        return results;
    }

    public Task DownloadAsync(
        string description,
        Func<IProgress<double>, CancellationToken, Task> work,
        CancellationToken cancellationToken) =>
        console.Progress()
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask(description, maxValue: 1.0);
                var progress = new Progress<double>(value => task.Value = value);
                await work(progress, cancellationToken).ConfigureAwait(false);
                task.Value = 1.0;
            });

    private static string StatusMarkup(CleanerStatus status) => status switch
    {
        CleanerStatus.NotApplicable => "[grey]n/a (other OS)[/]",
        CleanerStatus.NeedsElevation => "[yellow]needs admin[/]",
        CleanerStatus.Available => "[green]available[/]",
        CleanerStatus.NotFound => "[grey]not found[/]",
        _ => string.Empty,
    };
}

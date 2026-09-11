using Cleaner.Core.Utils;
using Spectre.Console;

namespace Cleaner.Cli.Rendering;

/// <summary>The tables: the cleaner catalogue, the scan preview, and the post-run summary.</summary>
public sealed partial class ConsoleRenderer
{
    public void CleanerList(IReadOnlyList<CleanerListEntry> entries)
    {
        // Grouped tables keep the large cleaner list scannable.
        var layout = ByLayout(entries, e => e.Cleaner.Category).ToList();
        foreach (var group in layout)
        {
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

        var categories = layout.Sum(g => g.Count());
        console.MarkupLine($"[grey]{entries.Count} cleaners across {categories} categories in {layout.Count} groups.[/]");
    }

    public void SizeTable(IReadOnlyList<ScanRow> rows, string sizeHeader, bool verbose = false)
    {
        // Keep unmeasurable cleaners visible without displaying a misleading zero.
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

        AppendTotal(table, rows.Sum(r => r.Result.TotalBytes), "yellow");
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

        AppendTotal(table, results.Sum(r => r.Result.BytesFreed), "green");
        console.Write(table);

        foreach (var row in results.Where(r => r.Result.HasErrors))
        {
            foreach (var error in row.Result.Errors)
            {
                console.MarkupLine($"[red]![/] [grey]{row.Cleaner.Id.EscapeMarkup()}:[/] {error.EscapeMarkup()}");
            }
        }
    }

    /// <summary>The trailing total row, padded out to whatever columns the table has.</summary>
    private static void AppendTotal(Table table, long bytes, string color)
    {
        var cells = new string[table.Columns.Count];
        Array.Fill(cells, string.Empty);
        cells[0] = "[bold]Total[/]";
        cells[1] = $"[bold {color}]{SizeFormatter.Humanize(bytes)}[/]";

        table.AddEmptyRow();
        table.AddRow(cells);
    }

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

using Cleaner.Core.Abstractions;

namespace Cleaner.Cli.Rendering;

/// <summary>
/// The CLI's entire view layer. Everything that touches Spectre.Console — markup, tables, prompts,
/// spinners, progress bars — lives behind this interface so the flows in
/// <see cref="Application.CleanerApp"/> stay rendering-free and testable.
/// </summary>
public interface IConsoleRenderer
{
    /// <summary>Write a single line of Spectre markup.</summary>
    void Line(string markup);

    /// <summary>The figlet banner, version, and tagline shown when the interactive menu opens.</summary>
    void InteractiveHeader(string version);

    /// <summary>Render the <c>list</c> table of cleaners, grouped by category, plus the footer count.</summary>
    void CleanerList(IReadOnlyList<CleanerListEntry> entries, int categoryCount);

    /// <summary>Render a name/size table for scan results under the given size-column header.</summary>
    void SizeTable(IReadOnlyList<ScanRow> rows, string sizeHeader);

    /// <summary>Render the post-clean summary table (freed bytes, status) and any error detail.</summary>
    void CleanSummary(IReadOnlyList<CleanRow> results);

    /// <summary>Ask a yes/no question.</summary>
    bool Confirm(string markup, bool defaultValue = false);

    /// <summary>Show the multi-select menu grouped by category; returns the chosen cleaners.</summary>
    IReadOnlyList<ICleaner> PromptSelection(IReadOnlyList<ICleaner> choosable);

    /// <summary>Run <paramref name="work"/> under a status spinner and return its result.</summary>
    Task<T> StatusAsync<T>(string status, Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken);

    /// <summary>Scan each cleaner under a spinner, surfacing the current cleaner's name.</summary>
    Task<IReadOnlyList<ScanRow>> ScanAsync(
        IReadOnlyList<ICleaner> cleaners,
        Func<ICleaner, Task<ScanResult>> scan,
        CancellationToken cancellationToken);

    /// <summary>Clean each cleaner under a progress bar, surfacing the current cleaner's name.</summary>
    Task<IReadOnlyList<CleanRow>> CleanAsync(
        IReadOnlyList<ICleaner> cleaners,
        Func<ICleaner, Task<CleanResult>> clean,
        CancellationToken cancellationToken);

    /// <summary>Run <paramref name="work"/> under a progress bar, feeding it a 0..1 progress sink.</summary>
    Task DownloadAsync(
        string description,
        Func<IProgress<double>, CancellationToken, Task> work,
        CancellationToken cancellationToken);
}

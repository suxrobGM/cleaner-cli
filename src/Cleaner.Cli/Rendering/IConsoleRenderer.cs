using Cleaner.Core.Abstractions;

namespace Cleaner.Cli.Rendering;

/// <summary>CLI view layer, keeping Spectre.Console details out of application flows.</summary>
public interface IConsoleRenderer
{
    /// <summary>False when output is redirected or no live terminal is attached (no prompts/spinners).</summary>
    bool IsInteractive { get; }

    void Line(string markup);

    /// <summary>The banner, tagline, and version shown when the menu opens.</summary>
    void InteractiveHeader(string version);

    /// <summary>One table per group, with a section per category.</summary>
    void CleanerList(IReadOnlyList<CleanerListEntry> entries);

    /// <summary>Waits for a key press; does nothing when not interactive.</summary>
    void Pause(string markup);

    void SizeTable(IReadOnlyList<ScanRow> rows, string sizeHeader, bool verbose = false);

    /// <summary>Show each cleaner's scan notes: protected paths left alone, refused scan roots.</summary>
    void ScanNotes(IReadOnlyList<ScanRow> rows);

    /// <summary>Post-run table of freed space and status, followed by any errors.</summary>
    void CleanSummary(IReadOnlyList<CleanRow> results);

    /// <summary>Asks a yes/no question; returns <paramref name="defaultValue"/> when not interactive.</summary>
    bool Confirm(string markup, bool defaultValue = false);

    MainMenuChoice PromptMainMenu();

    IReadOnlyList<ICleaner> PromptSelection(IReadOnlyList<ICleaner> choosable);

    /// <summary>Lets the user untick scanned folders; returns the ones kept, or null when not interactive.</summary>
    IReadOnlyList<string>? PromptFolders(IReadOnlyList<ScanRow> rows);

    Task<T> StatusAsync<T>(string status, Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken);

    /// <summary>Scans the cleaners in parallel under a spinner; results keep the input order.</summary>
    Task<IReadOnlyList<ScanRow>> ScanAsync(
        IReadOnlyList<ICleaner> cleaners,
        Func<ICleaner, Task<ScanResult>> scan,
        CancellationToken cancellationToken);

    /// <summary>Runs the cleaners one at a time under a progress bar.</summary>
    Task<IReadOnlyList<CleanRow>> CleanAsync(
        IReadOnlyList<ICleaner> cleaners,
        Func<ICleaner, Task<CleanResult>> clean,
        CancellationToken cancellationToken);

    /// <summary>Runs <paramref name="work"/> under a progress bar; it reports progress from 0 to 1.</summary>
    Task DownloadAsync(
        string description,
        Func<IProgress<double>, CancellationToken, Task> work,
        CancellationToken cancellationToken);
}

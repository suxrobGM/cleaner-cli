using Cleaner.Core.Abstractions;

namespace Cleaner.Cli.Rendering;

/// <summary>CLI view layer, keeping Spectre.Console details out of application flows.</summary>
public interface IConsoleRenderer
{
    /// <summary>False when output is redirected or no live terminal is attached (no prompts/spinners).</summary>
    bool IsInteractive { get; }

    /// <summary>Write a single line of Spectre markup.</summary>
    void Line(string markup);

    /// <summary>The figlet banner, version, and tagline shown when the interactive menu opens.</summary>
    void InteractiveHeader(string version);

    /// <summary>Render the cleaner list, one table per group and a section per category.</summary>
    void CleanerList(IReadOnlyList<CleanerListEntry> entries);

    /// <summary>Pauses interactive output until a key is pressed; no-op when redirected.</summary>
    void Pause(string markup);

    /// <summary>Renders scan sizes, optionally including per-target paths.</summary>
    void SizeTable(IReadOnlyList<ScanRow> rows, string sizeHeader, bool verbose = false);

    /// <summary>Render the post-clean summary table (freed bytes, status) and any error detail.</summary>
    void CleanSummary(IReadOnlyList<CleanRow> results);

    /// <summary>Ask a yes/no question.</summary>
    bool Confirm(string markup, bool defaultValue = false);

    /// <summary>Show the top-level menu and return the action the user picked.</summary>
    MainMenuChoice PromptMainMenu();

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

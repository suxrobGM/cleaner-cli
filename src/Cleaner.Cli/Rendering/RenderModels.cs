using Cleaner.Core.Abstractions;

namespace Cleaner.Cli.Rendering;

/// <summary>
/// A cleaner paired with its scan result, ready for size reporting. <paramref name="CommandBased"/>
/// marks rows whose size is only knowable after an external command runs, so the table can label
/// them instead of hiding them as 0 B.
/// </summary>
public sealed record ScanRow(ICleaner Cleaner, ScanResult Result, bool CommandBased = false);

/// <summary>A cleaner paired with its clean result, ready for the run summary.</summary>
public sealed record CleanRow(ICleaner Cleaner, CleanResult Result);

/// <summary>How a cleaner relates to the current machine, for the <c>list</c> status column.</summary>
public enum CleanerStatus
{
    NotApplicable,
    NeedsElevation,
    Available,
    NotFound,
}

/// <summary>A cleaner plus its resolved <see cref="CleanerStatus"/>, for listing.</summary>
public sealed record CleanerListEntry(ICleaner Cleaner, CleanerStatus Status);

/// <summary>The top-level actions offered by the interactive menu.</summary>
public enum MainMenuChoice
{
    /// <summary>Pick cleaners, preview the total, confirm, and delete.</summary>
    Clean,

    /// <summary>Pick cleaners and report what would be freed, deleting nothing.</summary>
    Preview,

    /// <summary>Show every cleaner and whether it applies to this machine.</summary>
    List,

    /// <summary>Check for a newer release and optionally install it.</summary>
    Update,

    /// <summary>Leave the menu.</summary>
    Exit,
}

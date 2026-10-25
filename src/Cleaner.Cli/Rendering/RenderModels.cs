using Cleaner.Core.Abstractions;

namespace Cleaner.Cli.Rendering;

/// <summary><paramref name="CommandBased"/> marks a command-based cleaner with no size until it runs.</summary>
public sealed record ScanRow(ICleaner Cleaner, ScanResult Result, bool CommandBased = false);

public sealed record CleanRow(ICleaner Cleaner, CleanResult Result);

/// <summary>How a cleaner relates to the current machine, for the <c>list</c> status column.</summary>
public enum CleanerStatus
{
    NotApplicable,
    NeedsElevation,
    Available,
    NotFound,
}

public sealed record CleanerListEntry(ICleaner Cleaner, CleanerStatus Status);

public enum MainMenuChoice
{
    /// <summary>Pick cleaners, preview the total, confirm, and delete.</summary>
    Clean,

    /// <summary>Show every cleaner and whether it applies to this machine.</summary>
    List,

    /// <summary>Check for a newer release and optionally install it.</summary>
    Update,

    Exit,
}

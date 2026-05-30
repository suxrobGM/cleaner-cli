using Cleaner.Core.Abstractions;

namespace Cleaner.Cli.Rendering;

/// <summary>A cleaner paired with its scan result, ready for size reporting.</summary>
public sealed record ScanRow(ICleaner Cleaner, ScanResult Result);

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

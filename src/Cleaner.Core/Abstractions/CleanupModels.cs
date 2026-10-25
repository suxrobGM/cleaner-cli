namespace Cleaner.Core.Abstractions;

/// <summary>How a cleanup target is removed.</summary>
public enum DeleteMode
{
    DeleteDirectory,

    /// <summary>Delete everything inside the directory but keep the directory itself.</summary>
    ClearContents,

    DeleteFile,

    /// <summary>Reclaimed by the cleaner's own tool; the path is only a preview label and is never deleted.</summary>
    ToolManaged,
}

/// <summary>A single location a cleaner can remove, with its measured size.</summary>
public sealed record CleanupTarget(string Path, long Bytes, DeleteMode Mode, string? Description = null);

/// <summary>The targets a cleaner would remove.</summary>
/// <param name="Targets">What the cleaner found it could remove.</param>
/// <param name="ToolUnavailable">
/// The backing tool is on PATH but did not answer, so the cleaner has nothing to offer this run.
/// </param>
public sealed record ScanResult(IReadOnlyList<CleanupTarget> Targets, bool ToolUnavailable = false)
{
    public long TotalBytes => Targets.Sum(t => t.Bytes);

    public int ItemCount => Targets.Count;

    public bool IsEmpty => Targets.Count == 0;

    /// <summary>Messages for the user, such as paths left alone as protected.</summary>
    public IReadOnlyList<string> Notes { get; init; } = [];

    public static ScanResult Empty { get; } = new([]);

    public static ScanResult Unavailable { get; } = new([], ToolUnavailable: true);
}

/// <summary>What happened to one path during a clean.</summary>
public enum OutcomeKind
{
    Removed,

    /// <summary>Left alone on purpose: protected, in use, or gone since the scan.</summary>
    Skipped,

    Failed,

    /// <summary>The cleaner's own tool ran instead of a direct deletion.</summary>
    CommandRun,
}

/// <summary>What happened to one path during a clean, and why.</summary>
public sealed record TargetOutcome(string Path, OutcomeKind Kind, long Bytes = 0, string? Reason = null);

/// <summary>How much a cleaner freed and any errors it hit.</summary>
public sealed record CleanResult(long BytesFreed, int ItemsRemoved, IReadOnlyList<string> Errors)
{
    public bool HasErrors => Errors.Count > 0;

    /// <summary>Everything removed, skipped, or failed during the run.</summary>
    public IReadOnlyList<TargetOutcome> Outcomes { get; init; } = [];

    public int SkippedCount => Outcomes.Count(o => o.Kind == OutcomeKind.Skipped);

    public static CleanResult Empty { get; } = new(0, 0, []);
}

public readonly record struct CleanProgress(string Description, long BytesFreed);

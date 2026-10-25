namespace Cleaner.Core.Abstractions;

/// <summary>How a cleanup target is removed.</summary>
public enum DeleteMode
{
    /// <summary>Remove the directory and everything under it.</summary>
    DeleteDirectory,

    /// <summary>Delete everything inside the directory but keep the directory itself.</summary>
    ClearContents,

    /// <summary>Delete a single file rather than a directory (e.g. a multi-GB telemetry database).</summary>
    DeleteFile,

    /// <summary>
    /// Reclaimed by the cleaner's own tool (a prune command, a disk compaction). The path is only a
    /// label for the preview and is never deleted.
    /// </summary>
    ToolManaged,
}

/// <summary>A single location a cleaner can remove, with its measured size.</summary>
public sealed record CleanupTarget(string Path, long Bytes, DeleteMode Mode, string? Description = null);

/// <summary>The result of scanning a cleaner: the targets it would remove and their total size.</summary>
/// <param name="Targets">What the cleaner found it could remove.</param>
/// <param name="ToolUnavailable">
/// Set when the scan reached out to the backing tool and it could not answer, so the cleaner has
/// nothing to offer this run even though its executable is on PATH.
/// </param>
public sealed record ScanResult(IReadOnlyList<CleanupTarget> Targets, bool ToolUnavailable = false)
{
    public long TotalBytes => Targets.Sum(t => t.Bytes);

    public int ItemCount => Targets.Count;

    public bool IsEmpty => Targets.Count == 0;

    /// <summary>Things the user should know about this scan, such as paths left alone as protected.</summary>
    public IReadOnlyList<string> Notes { get; init; } = [];

    public static ScanResult Empty { get; } = new([]);

    /// <summary>The backing tool did not answer; nothing to scan and nothing to clean.</summary>
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

/// <summary>One line of the clean's record: a path, what happened to it, and why.</summary>
public sealed record TargetOutcome(string Path, OutcomeKind Kind, long Bytes = 0, string? Reason = null);

/// <summary>The result of running a cleaner: how much was freed and any errors encountered.</summary>
public sealed record CleanResult(long BytesFreed, int ItemsRemoved, IReadOnlyList<string> Errors)
{
    public bool HasErrors => Errors.Count > 0;

    /// <summary>Per-path record of the run: everything removed, skipped, or failed.</summary>
    public IReadOnlyList<TargetOutcome> Outcomes { get; init; } = [];

    public int SkippedCount => Outcomes.Count(o => o.Kind == OutcomeKind.Skipped);

    public static CleanResult Empty { get; } = new(0, 0, []);
}

/// <summary>Progress emitted while a cleaner runs, so the UI can render a live bar.</summary>
public readonly record struct CleanProgress(string Description, long BytesFreed);

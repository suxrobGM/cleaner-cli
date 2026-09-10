namespace Cleaner.Core.Abstractions;

/// <summary>A single location a cleaner can remove, with its measured size.</summary>
public sealed record CleanupTarget(string Path, long Bytes, string? Description = null);

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

    public static ScanResult Empty { get; } = new([]);

    /// <summary>The backing tool did not answer; nothing to scan and nothing to clean.</summary>
    public static ScanResult Unavailable { get; } = new([], ToolUnavailable: true);
}

/// <summary>The result of running a cleaner: how much was freed and any errors encountered.</summary>
public sealed record CleanResult(long BytesFreed, int ItemsRemoved, IReadOnlyList<string> Errors)
{
    public bool HasErrors => Errors.Count > 0;

    public static CleanResult Empty { get; } = new(0, 0, []);
}

/// <summary>Progress emitted while a cleaner runs, so the UI can render a live bar.</summary>
public readonly record struct CleanProgress(string Description, long BytesFreed);

namespace Cleaner.Core.Abstractions;

/// <summary>A single location a cleaner can remove, with its measured size.</summary>
public sealed record CleanupTarget(string Path, long Bytes, string? Description = null);

/// <summary>The result of scanning a cleaner: the targets it would remove and their total size.</summary>
public sealed record ScanResult(IReadOnlyList<CleanupTarget> Targets)
{
    public long TotalBytes => Targets.Sum(t => t.Bytes);

    public int ItemCount => Targets.Count;

    public bool IsEmpty => Targets.Count == 0;

    public static ScanResult Empty { get; } = new([]);
}

/// <summary>The result of running a cleaner: how much was freed and any errors encountered.</summary>
public sealed record CleanResult(long BytesFreed, int ItemsRemoved, IReadOnlyList<string> Errors)
{
    public bool HasErrors => Errors.Count > 0;

    public static CleanResult Empty { get; } = new(0, 0, []);
}

/// <summary>Progress emitted while a cleaner runs, so the UI can render a live bar.</summary>
public readonly record struct CleanProgress(string Description, long BytesFreed);

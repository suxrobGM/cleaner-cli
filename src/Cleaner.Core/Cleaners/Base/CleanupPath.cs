namespace Cleaner.Core.Cleaners.Base;

/// <summary>How a <see cref="CleanupPath"/> should be removed.</summary>
public enum DeleteMode
{
    /// <summary>Remove the directory and everything under it.</summary>
    DeleteDirectory,

    /// <summary>Delete everything inside the directory but keep the directory itself.</summary>
    ClearContents,

    /// <summary>Delete a single file rather than a directory (e.g. a multi-GB telemetry database).</summary>
    DeleteFile,
}

/// <summary>A single file or directory a cleaner targets, and how to remove it.</summary>
public readonly record struct CleanupPath(
    string Path,
    DeleteMode Mode = DeleteMode.DeleteDirectory,
    string? Description = null);

namespace Cleaner.Core.Cleaners.Base;

/// <summary>How a <see cref="CleanupPath"/> should be removed.</summary>
public enum DeleteMode
{
    /// <summary>Remove the directory and everything under it.</summary>
    DeleteDirectory,

    /// <summary>Delete everything inside the directory but keep the directory itself.</summary>
    ClearContents,
}

/// <summary>A single directory a cleaner targets, and how to remove it.</summary>
public readonly record struct CleanupPath(
    string Path,
    DeleteMode Mode = DeleteMode.DeleteDirectory,
    string? Description = null);

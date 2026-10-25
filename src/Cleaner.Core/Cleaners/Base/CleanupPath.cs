using Cleaner.Core.Abstractions;

namespace Cleaner.Core.Cleaners.Base;

/// <summary>A single file or directory a cleaner targets, and how to remove it.</summary>
public readonly record struct CleanupPath(
    string Path,
    DeleteMode Mode = DeleteMode.DeleteDirectory,
    string? Description = null);

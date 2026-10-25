namespace Cleaner.Core.Services;

/// <summary>
/// The one place that decides whether a path may be deleted. Every cleaner's targets pass through
/// it twice, once when scanned and again just before removal, and nothing can loosen it.
/// </summary>
public interface IDeletionGuard
{
    /// <summary>
    /// Why <paramref name="path"/> must not be deleted, or null when it may be. With
    /// <paramref name="contentsOnly"/> the path is a folder whose children are cleared one by one,
    /// so containing something protected is fine; each child is checked on its own.
    /// </summary>
    string? WhyProtected(string path, bool contentsOnly = false);

    /// <summary>Why <paramref name="path"/> is busy (a running program lives under it), or null.</summary>
    string? WhyInUse(string path);

    /// <summary>Why a workspace sweep must not start at <paramref name="root"/>, or null when it may.</summary>
    string? WhyNotProjectRoot(string root);

    /// <summary>Whether a workspace sweep may look inside <paramref name="directory"/>.</summary>
    bool CanEnter(string directory);

    /// <summary>A guard that also protects <paramref name="paths"/>, for folders the user chose to keep.</summary>
    IDeletionGuard Excluding(IEnumerable<string> paths);
}

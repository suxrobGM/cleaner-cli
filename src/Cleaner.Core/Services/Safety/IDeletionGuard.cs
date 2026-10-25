namespace Cleaner.Core.Services;

/// <summary>
/// The one place that decides whether a path may be deleted. Every target is checked at scan time and
/// again just before removal, and the guard can only be tightened, never loosened.
/// </summary>
public interface IDeletionGuard
{
    /// <summary>
    /// Why <paramref name="path"/> must not be deleted, or null when it may be. With
    /// <paramref name="contentsOnly"/>, containing something protected is fine because each child is
    /// checked on its own.
    /// </summary>
    string? WhyProtected(string path, bool contentsOnly = false);

    /// <summary>Why <paramref name="path"/> is busy (a running program lives under it), or null.</summary>
    string? WhyInUse(string path);

    /// <summary>Why a project search must not start at <paramref name="root"/>, or null when it may.</summary>
    string? WhyNotProjectRoot(string root);

    /// <summary>Whether a project search may look inside <paramref name="directory"/>.</summary>
    bool CanEnter(string directory);

    /// <summary>A guard that also protects <paramref name="paths"/>, for folders the user chose to keep.</summary>
    IDeletionGuard Excluding(IEnumerable<string> paths);
}

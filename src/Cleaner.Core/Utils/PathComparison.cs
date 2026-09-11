namespace Cleaner.Core.Utils;

/// <summary>How paths are matched, shared so callers can't disagree about what a path is.</summary>
public static class PathComparison
{
    /// <summary>Unifies separators so one folder spelled two ways compares equal.</summary>
    public static string Normalize(string path) =>
        path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

    /// <summary>Linux paths are case-sensitive; Windows and (default) macOS volumes are not.</summary>
    public static StringComparer ComparerFor(bool isLinux) =>
        isLinux ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

    /// <summary>Normalized paths in a set that compares them the way the platform does.</summary>
    public static IReadOnlySet<string> CreateSet(IEnumerable<string> paths, bool isLinux) =>
        new HashSet<string>(paths.Select(Normalize), ComparerFor(isLinux));

    /// <summary>
    /// Whether a path survived the user's picks. The one place that knows a selection must be
    /// looked up normalized, and that a null selection takes everything.
    /// </summary>
    public static bool IsSelected(IReadOnlySet<string>? selected, string path) =>
        selected is null || selected.Contains(Normalize(path));
}

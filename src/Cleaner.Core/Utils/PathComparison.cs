namespace Cleaner.Core.Utils;

/// <summary>Shared path matching, so callers can't disagree about what a path is.</summary>
/// <remarks>
/// Pure string logic that never asks the OS, so Windows paths compare correctly on a Linux test runner
/// and vice versa.
/// </remarks>
public static class PathComparison
{
    /// <summary>Unifies separators so one folder spelled two ways compares equal.</summary>
    public static string Normalize(string path) =>
        path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

    /// <summary>Linux paths are case-sensitive; Windows and (default) macOS volumes are not.</summary>
    public static StringComparer ComparerFor(bool isLinux) =>
        isLinux ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;

    /// <inheritdoc cref="ComparerFor"/>
    public static StringComparison ComparisonFor(bool isLinux) =>
        isLinux ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

    /// <summary>Normalized paths in a set that compares them the way the platform does.</summary>
    public static IReadOnlySet<string> CreateSet(IEnumerable<string> paths, bool isLinux) =>
        new HashSet<string>(paths.Select(Normalize), ComparerFor(isLinux));

    /// <summary>Whether the user selected the path; a null selection means everything.</summary>
    public static bool IsSelected(IReadOnlySet<string>? selected, string path) =>
        selected is null || selected.Contains(Normalize(path));

    /// <summary>
    /// A comparison key: forward slashes, no repeated or trailing separators. A filesystem root
    /// becomes <c>""</c> and a drive root becomes <c>"C:"</c>, so "is under" is a plain prefix test.
    /// </summary>
    public static string Key(string path)
    {
        var unified = path.Trim().Replace('\\', '/');
        var unc = unified.StartsWith("//", StringComparison.Ordinal);
        var parts = unified.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var joined = string.Join('/', parts);
        return unc ? "//" + joined : unified.StartsWith('/') ? "/" + joined : joined;
    }

    /// <summary>True for <c>/</c>, <c>C:\</c>, and other volume roots.</summary>
    public static bool IsRoot(string path) => IsRootKey(Key(path));

    /// <inheritdoc cref="IsRoot"/>
    public static bool IsRootKey(string key) =>
        key is "/" or "" || (key.Length == 2 && key[1] == ':') || (key.StartsWith("//") && key.Count(c => c == '/') <= 3);

    /// <summary>Absolute on either platform:<c>/x</c>, <c>\x</c>, <c>C:\x</c>, or <c>\\server\share</c>.</summary>
    public static bool IsRooted(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var p = path.Trim();
        return p[0] is '/' or '\\' || (p.Length >= 3 && char.IsAsciiLetter(p[0]) && p[1] == ':' && p[2] is '/' or '\\');
    }

    public static bool IsSameOrUnder(string path, string root, bool isLinux) =>
        IsKeySameOrUnder(Key(path), Key(root), ComparisonFor(isLinux));

    /// <summary><paramref name="pathKey"/> equals <paramref name="rootKey"/> or lies under it; both are <see cref="Key"/>s.</summary>
    public static bool IsKeySameOrUnder(string pathKey, string rootKey, StringComparison comparison)
    {
        var p = TrimRoot(pathKey);
        var r = TrimRoot(rootKey);
        return p.Equals(r, comparison)
            || (p.Length > r.Length && p.StartsWith(r, comparison) && p[r.Length] == '/');
    }

    /// <summary>The parent directory by string logic, or null at a root.</summary>
    public static string? Parent(string path)
    {
        var key = Key(path);
        var cut = key.LastIndexOf('/');
        if (IsRoot(path) || cut < 0)
        {
            return null;
        }

        // Keep the separator style of the input so the result can be handed back to the OS.
        var separator = path.Contains('\\') ? '\\' : '/';
        var parent = cut == 0 ? "/" : key[..cut];
        if (parent.Length == 2 && parent[1] == ':')
        {
            parent += "/";
        }

        return parent.Replace('/', separator);
    }

    public static string Name(string path)
    {
        var key = Key(path);
        return key[(key.LastIndexOf('/') + 1)..];
    }

    // Map "/" to "" so the prefix test still finds a separator right after the root; "C:" needs no change.
    private static string TrimRoot(string key) => key == "/" ? string.Empty : key;
}

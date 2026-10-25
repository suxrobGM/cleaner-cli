using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Cleaner.Core.Utils;

namespace Cleaner.Core.Services;

/// <inheritdoc cref="IDeletionGuard"/>
/// <remarks>
/// Immutable and safe to share across the parallel scan. Paths are compared as strings (see
/// <see cref="PathComparison"/>) except for link resolution, which asks the filesystem and is
/// cached per directory.
/// </remarks>
public sealed partial class DeletionGuard : IDeletionGuard
{
    /// <summary>Where users list extra folders Cleaner must never touch, one path per line.</summary>
    public const string ExcludeFileName = "exclude.txt";

    private readonly IFileSystemService _fs;
    private readonly StringComparison _comparison;
    private readonly string[] _essential;
    private readonly string[] _notWorkspaces;
    private readonly string[] _pathDirectories;
    private readonly string[] _strictPathDirectories;
    private readonly KeyedRoot[] _installRoots;
    private readonly Exclusion[] _exclusions;
    private readonly Lazy<string[]> _running;
    private readonly ConcurrentDictionary<string, string> _realDirectories;

    private DeletionGuard(
        IFileSystemService fs,
        bool isLinux,
        string[] essential,
        string[] notWorkspaces,
        string[] pathDirectories,
        string[] strictPathDirectories,
        KeyedRoot[] installRoots,
        Exclusion[] exclusions,
        Lazy<string[]> running,
        ConcurrentDictionary<string, string> realDirectories)
    {
        _fs = fs;
        _comparison = isLinux ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        _essential = essential;
        _notWorkspaces = notWorkspaces;
        _pathDirectories = pathDirectories;
        _strictPathDirectories = strictPathDirectories;
        _installRoots = installRoots;
        _exclusions = exclusions;
        _running = running;
        _realDirectories = realDirectories;
    }

    /// <summary>Builds the guard for this machine: its PATH, install roots, and the user's exclude file.</summary>
    public static DeletionGuard Create(IEnvironmentService env, IFileSystemService fs, IProcessRunner processRunner)
    {
        var essential = Keys(ProtectedPaths.Essential(env));

        // A PATH entry that is a link (nvm's NVM_SYMLINK) protects the folder it points at, too.
        var pathDirectories = env.PathDirectories
            .Concat(env.PathDirectories.Select(fs.ResolveLinkTarget).OfType<string>())
            .Where(PathComparison.IsRooted)
            .ToList();
        var pathKeys = Keys(pathDirectories);

        var windows = env.WindowsDirectory is { } w ? PathComparison.Key(w) : null;
        var comparison = env.IsLinux ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        // "Inside a PATH entry" is only meaningful for entries that are a tool's own folder. A root,
        // the profile, or the Windows tree is on PATH too, and those hold caches Cleaner may clear.
        var strict = pathKeys
            .Where(key => key.Length > 0 && !(key.Length == 2 && key[1] == ':'))
            .Where(key => !essential.Any(e => Under(e, key, comparison)))
            .Where(key => windows is null || !Under(key, windows, comparison))
            .ToArray();

        var installRoots = ProtectedPaths.InstallRoots(env)
            .Where(r => PathComparison.IsRooted(r.Path))
            .GroupBy(r => PathComparison.Key(r.Path), StringComparer.FromComparison(comparison))
            .Select(g => new KeyedRoot(
                g.Key,
                g.First().Path,
                g.First().Reason,
                // The same folder named twice keeps only the holes both entries allow.
                g.Select(r => r.Holes.Select(h => PathComparison.Key(g.Key + "/" + h)))
                    .Aggregate((a, b) => a.Intersect(b, StringComparer.FromComparison(comparison)))
                    .ToArray()))
            .ToArray();

        var excludeFile = Path.Combine(env.HomeDirectory, ".cleaner", ExcludeFileName);
        var exclusions = ReadExcludeFile(env, fs, excludeFile)
            .Select(path => new Exclusion(PathComparison.Key(path), path, $"listed in {excludeFile}"))
            .ToArray();

        return new DeletionGuard(
            fs,
            env.IsLinux,
            essential,
            Keys(ProtectedPaths.NotWorkspaces(env)),
            pathKeys,
            strict,
            installRoots,
            exclusions,
            new Lazy<string[]>(() => [.. processRunner.RunningExecutablePaths().Select(PathComparison.Key)]),
            new ConcurrentDictionary<string, string>(StringComparer.FromComparison(comparison)));
    }

    public IDeletionGuard WithRunExclusions(IEnumerable<string> paths) =>
        new DeletionGuard(
            _fs,
            _comparison == StringComparison.Ordinal,
            _essential,
            _notWorkspaces,
            _pathDirectories,
            _strictPathDirectories,
            _installRoots,
            [.. _exclusions, .. paths.Select(p => new Exclusion(PathComparison.Key(p), p, "you chose to keep it"))],
            _running,
            _realDirectories);

    public string? WhyProtected(string path, bool contentsOnly = false)
    {
        if (!PathComparison.IsRooted(path))
        {
            return "is not an absolute path";
        }

        if (Check(path, contentsOnly) is { } reason)
        {
            return reason;
        }

        if (_fs.IsLink(path))
        {
            return "is a link; deleting through it would reach whatever it points to";
        }

        // Reached through a junction (scoop's `current`, nvm's symlink), the real folder decides.
        return RealPath(path) is { } real && !Same(real, path) && Check(real, contentsOnly) is { } viaLink
            ? $"resolves to {real}, which {viaLink}"
            : null;
    }

    public string? WhyInUse(string path)
    {
        var key = PathComparison.Key(path);
        var running = _running.Value.FirstOrDefault(exe => Under(exe, key, _comparison));
        return running is null ? null : $"is in use by the running program {running}";
    }

    public string? WhyUnusableWorkspaceRoot(string root)
    {
        if (!PathComparison.IsRooted(root))
        {
            return "is not an absolute path";
        }

        var key = PathComparison.Key(root);
        if (PathComparison.IsRoot(root))
        {
            return "is a drive root";
        }

        if (_essential.FirstOrDefault(e => Under(e, key, _comparison)) is { } essential)
        {
            return Same(essential, key) ? "is a system or profile folder" : $"contains the system or profile folder {essential}";
        }

        if (_notWorkspaces.FirstOrDefault(n => Under(key, n, _comparison)) is { } system)
        {
            return $"is inside {system}, which holds installed software rather than projects";
        }

        return _installRoots.FirstOrDefault(r => Under(key, r.Key, _comparison)) is { } install
            ? $"is inside {install.Reason} ({install.Path})"
            : null;
    }

    public bool CanDescend(string directory)
    {
        var leaf = PathComparison.Leaf(directory);
        if (leaf.StartsWith('.') || leaf.Equals("node_modules", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var key = PathComparison.Key(directory);
        return !_installRoots.Any(r => Under(key, r.Key, _comparison))
            && !_pathDirectories.Any(p => Same(p, key));
    }

    /// <summary>Every rule that can be decided from the path string alone, plus the <c>.git</c> probe.</summary>
    private string? Check(string path, bool contentsOnly)
    {
        var key = PathComparison.Key(path);

        if (PathComparison.IsRoot(path))
        {
            return "is a drive root";
        }

        if (_essential.FirstOrDefault(e => Under(e, key, _comparison)) is { } essential)
        {
            return Same(essential, key) ? "is a system or profile folder" : $"contains the system or profile folder {essential}";
        }

        foreach (var exclusion in _exclusions)
        {
            // Clearing a folder's contents is fine when only one child is excluded; that child is
            // checked on its own.
            var hit = contentsOnly
                ? Under(key, exclusion.Key, _comparison)
                : Under(key, exclusion.Key, _comparison) || Under(exclusion.Key, key, _comparison);
            if (hit)
            {
                return $"is excluded ({exclusion.Source}: {exclusion.Path})";
            }
        }

        if (!contentsOnly && _pathDirectories.FirstOrDefault(p => Under(p, key, _comparison)) is { } pathDir)
        {
            return Same(pathDir, key) ? "is on PATH" : $"contains the PATH folder {pathDir}";
        }

        if (_strictPathDirectories.FirstOrDefault(p => Under(key, p, _comparison) && !Same(key, p)) is { } owner)
        {
            return $"is inside the PATH folder {owner}";
        }

        foreach (var root in _installRoots)
        {
            if (!contentsOnly && Under(root.Key, key, _comparison))
            {
                return Same(root.Key, key) ? $"is {root.Reason}" : $"contains {root.Reason} ({root.Path})";
            }

            if (Under(key, root.Key, _comparison) && !Same(key, root.Key) && !root.Holes.Any(h => Under(key, h, _comparison)))
            {
                return $"is inside {root.Reason} ({root.Path})";
            }
        }

        if (key.Split('/').Any(segment => segment.Equals(".git", StringComparison.OrdinalIgnoreCase)))
        {
            return "is inside a .git folder";
        }

        var gitChild = Path.Combine(path, ".git");
        return _fs.DirectoryExists(gitChild) || _fs.FileExists(gitChild) ? "is a git repository" : null;
    }

    /// <summary>The path with every linked ancestor replaced by what it points to, or null when unknown.</summary>
    private string? RealPath(string path) =>
        PathComparison.Parent(path) is { } parent && RealDirectory(parent, depth: 0) is { } realParent
            ? Path.Combine(realParent, PathComparison.Leaf(path))
            : null;

    private string? RealDirectory(string directory, int depth)
    {
        if (depth > 40 || PathComparison.IsRoot(directory))
        {
            return directory;
        }

        var key = PathComparison.Key(directory);
        if (_realDirectories.TryGetValue(key, out var cached))
        {
            return cached;
        }

        string? real;
        if (_fs.ResolveLinkTarget(directory) is { } target)
        {
            real = RealDirectory(target, depth + 1);
        }
        else
        {
            var parent = PathComparison.Parent(directory);
            var realParent = parent is null ? null : RealDirectory(parent, depth + 1);
            real = realParent is null || Same(realParent, parent!)
                ? directory
                : Path.Combine(realParent, PathComparison.Leaf(directory));
        }

        if (real is not null)
        {
            _realDirectories[key] = real;
        }

        return real;
    }

    private bool Same(string a, string b) =>
        PathComparison.Key(a).Equals(PathComparison.Key(b), _comparison);

    /// <summary><paramref name="path"/> equals <paramref name="root"/> or lies under it; both are keys.</summary>
    private static bool Under(string path, string root, StringComparison comparison)
    {
        path = path == "/" ? string.Empty : path;
        root = root == "/" ? string.Empty : root;
        return path.Equals(root, comparison)
            || (path.Length > root.Length && path.StartsWith(root, comparison) && path[root.Length] == '/');
    }

    private static string[] Keys(IEnumerable<string> paths) =>
        [.. paths.Where(p => !string.IsNullOrWhiteSpace(p) && PathComparison.IsRooted(p)).Select(PathComparison.Key).Distinct()];

    /// <summary>
    /// The user's exclude file: one path per line, <c>#</c> comments, <c>~</c> and
    /// <c>%VAR%</c>/<c>$VAR</c> expansion. Lines that don't name an absolute path are ignored.
    /// </summary>
    internal static IEnumerable<string> ReadExcludeFile(IEnvironmentService env, IFileSystemService fs, string file)
    {
        foreach (var raw in fs.ReadAllLines(file))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var expanded = ExpandVariables(env, line);
            if (expanded == "~" || expanded.StartsWith("~/", StringComparison.Ordinal) || expanded.StartsWith("~\\", StringComparison.Ordinal))
            {
                expanded = env.HomeDirectory + expanded[1..];
            }

            if (PathComparison.IsRooted(expanded))
            {
                yield return expanded;
            }
        }
    }

    private static string ExpandVariables(IEnvironmentService env, string text) =>
        VariablePattern().Replace(
            text,
            m =>
            {
                var name = m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Success ? m.Groups[2].Value : m.Groups[3].Value;
                return env.GetEnvironmentVariable(name) ?? (name is "HOME" or "USERPROFILE" ? env.HomeDirectory : m.Value);
            });

    [GeneratedRegex(@"%(\w+)%|\$\{(\w+)\}|\$(\w+)")]
    private static partial Regex VariablePattern();

    private sealed record KeyedRoot(string Key, string Path, string Reason, string[] Holes);

    private sealed record Exclusion(string Key, string Path, string Source);
}

using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Cleaner.Core.Utils;

namespace Cleaner.Core.Services;

/// <inheritdoc cref="IDeletionGuard"/>
/// <remarks>
/// Immutable, so the parallel scan can share it. Rules compare path strings; only link resolution
/// touches the filesystem, and it is cached per directory.
/// </remarks>
public sealed partial class DeletionGuard : IDeletionGuard
{
    /// <summary>Where users list extra folders Cleaner must never touch, one path per line.</summary>
    public const string ExcludeFileName = "exclude.txt";

    private readonly IFileSystemService _fs;
    private readonly Rules _rules;

    private DeletionGuard(IFileSystemService fs, Rules rules)
    {
        _fs = fs;
        _rules = rules;
    }

    /// <summary>Builds the guard for this machine: its PATH, install roots, and the user's exclude file.</summary>
    public static DeletionGuard Create(IEnvironmentService env, IFileSystemService fs, IProcessRunner processRunner)
    {
        var essential = Keys(ProtectedPaths.SystemFolders(env));

        // A PATH entry that is a link (nvm's NVM_SYMLINK) protects the folder it points at, too.
        var pathDirectories = env.PathDirectories
            .Concat(env.PathDirectories.Select(fs.ResolveLinkTarget).OfType<string>())
            .Where(PathComparison.IsRooted)
            .ToList();
        var pathKeys = Keys(pathDirectories);

        var windows = env.WindowsDirectory is { } w ? PathComparison.Key(w) : null;
        var comparison = PathComparison.ComparisonFor(env.IsLinux);

        // Only a tool's own PATH folder protects what's inside it. Drive roots, the profile, and the
        // Windows tree can be on PATH too, and they hold caches Cleaner may clear.
        var strict = pathKeys
            .Where(key => key.Length > 0 && !(key.Length == 2 && key[1] == ':'))
            .Where(key => !essential.Any(e => PathComparison.IsKeySameOrUnder(e, key, comparison)))
            .Where(key => windows is null || !PathComparison.IsKeySameOrUnder(key, windows, comparison))
            .ToArray();

        var installs = ProtectedPaths.Installs(env)
            .Where(r => PathComparison.IsRooted(r.Path))
            .GroupBy(r => PathComparison.Key(r.Path), StringComparer.FromComparison(comparison))
            .Select(g => new KeyedRoot(
                g.Key,
                g.First().Path,
                g.First().Reason,
                // A folder listed twice keeps only the cache folders both entries allow.
                g.Select(r => r.CacheFolders.Select(h => PathComparison.Key(g.Key + "/" + h)))
                    .Aggregate((a, b) => a.Intersect(b, StringComparer.FromComparison(comparison)))
                    .ToArray()))
            .ToArray();

        var excludeFile = Path.Combine(env.HomeDirectory, ".cleaner", ExcludeFileName);
        var exclusions = ReadExcludeFile(env, fs, excludeFile)
            .Select(path => new Exclusion(PathComparison.Key(path), path, $"listed in {excludeFile}"))
            .ToArray();

        return new DeletionGuard(fs, new Rules(
            comparison,
            essential,
            Keys(ProtectedPaths.NonProjectFolders(env)),
            pathKeys,
            strict,
            installs,
            exclusions,
            new Lazy<string[]>(() => [.. processRunner.RunningExecutablePaths().Select(PathComparison.Key)]),
            new ConcurrentDictionary<string, string>(StringComparer.FromComparison(comparison))));
    }

    public IDeletionGuard Excluding(IEnumerable<string> paths) =>
        new DeletionGuard(_fs, _rules with
        {
            Exclusions = [.. _rules.Exclusions, .. paths.Select(p => new Exclusion(PathComparison.Key(p), p, "you chose to keep it"))],
        });

    public string? WhyProtected(string path, bool contentsOnly = false)
    {
        if (!PathComparison.IsRooted(path))
        {
            return "is not an absolute path";
        }

        var key = PathComparison.Key(path);
        if (Check(path, key, contentsOnly) is { } reason)
        {
            return reason;
        }

        if (_fs.IsLink(path))
        {
            return "is a link; deleting through it would reach whatever it points to";
        }

        // Behind a junction (scoop's `current`, nvm's symlink), the real folder must pass too.
        return RealPath(path) is { } real && PathComparison.Key(real) is var realKey && !Same(realKey, key)
            && Check(real, realKey, contentsOnly) is { } viaLink
            ? $"resolves to {real}, which {viaLink}"
            : null;
    }

    public string? WhyInUse(string path)
    {
        var key = PathComparison.Key(path);
        var running = _rules.Running.Value.FirstOrDefault(exe => Under(exe, key));
        return running is null ? null : $"is in use by the running program {running}";
    }

    public string? WhyNotProjectRoot(string root)
    {
        if (!PathComparison.IsRooted(root))
        {
            return "is not an absolute path";
        }

        var key = PathComparison.Key(root);
        if (SystemFolderReason(key) is { } reason)
        {
            return reason;
        }

        if (_rules.NonProjectFolders.FirstOrDefault(n => Under(key, n)) is { } system)
        {
            return $"is inside {system}, which holds installed software rather than projects";
        }

        return _rules.Installs.FirstOrDefault(r => Under(key, r.Key)) is { } install
            ? $"is inside {install.Reason} ({install.Path})"
            : null;
    }

    public bool CanEnter(string directory)
    {
        var leaf = PathComparison.Name(directory);
        if (leaf.StartsWith('.') || leaf.Equals("node_modules", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var key = PathComparison.Key(directory);
        return !_rules.Installs.Any(r => Under(key, r.Key))
            && !_rules.PathDirectories.Any(p => Same(p, key));
    }

    /// <summary>A drive root, a system or profile folder, or a folder holding one.</summary>
    private string? SystemFolderReason(string key)
    {
        if (PathComparison.IsRootKey(key))
        {
            return "is a drive root";
        }

        return _rules.SystemFolders.FirstOrDefault(e => Under(e, key)) is { } essential
            ? Same(essential, key) ? "is a system or profile folder" : $"contains the system or profile folder {essential}"
            : null;
    }

    /// <summary>Every rule that can be decided from the path string alone, plus the <c>.git</c> probe.</summary>
    private string? Check(string path, string key, bool contentsOnly)
    {
        if (SystemFolderReason(key) is { } reason)
        {
            return reason;
        }

        foreach (var exclusion in _rules.Exclusions)
        {
            // Clearing a folder's contents is fine when only one child is excluded; that child is
            // checked on its own.
            var hit = contentsOnly
                ? Under(key, exclusion.Key)
                : Under(key, exclusion.Key) || Under(exclusion.Key, key);
            if (hit)
            {
                return $"is excluded ({exclusion.Source}: {exclusion.Path})";
            }
        }

        if (!contentsOnly && _rules.PathDirectories.FirstOrDefault(p => Under(p, key)) is { } pathDir)
        {
            return Same(pathDir, key) ? "is on PATH" : $"contains the PATH folder {pathDir}";
        }

        if (_rules.StrictPathDirectories.FirstOrDefault(p => Under(key, p) && !Same(key, p)) is { } owner)
        {
            return $"is inside the PATH folder {owner}";
        }

        foreach (var root in _rules.Installs)
        {
            if (!contentsOnly && Under(root.Key, key))
            {
                return Same(root.Key, key) ? $"is {root.Reason}" : $"contains {root.Reason} ({root.Path})";
            }

            if (Under(key, root.Key) && !Same(key, root.Key) && !root.CacheFolders.Any(h => Under(key, h)))
            {
                return $"is inside {root.Reason} ({root.Path})";
            }
        }

        if ($"/{key}/".Contains("/.git/", StringComparison.OrdinalIgnoreCase))
        {
            return "is inside a .git folder";
        }

        var gitChild = Path.Combine(path, ".git");
        return _fs.DirectoryExists(gitChild) || _fs.FileExists(gitChild) ? "is a git repository" : null;
    }

    /// <summary>The path with every linked ancestor replaced by what it points to, or null when unknown.</summary>
    private string? RealPath(string path) =>
        PathComparison.Parent(path) is { } parent && RealDirectory(parent, depth: 0) is { } realParent
            ? Path.Combine(realParent, PathComparison.Name(path))
            : null;

    private string? RealDirectory(string directory, int depth)
    {
        if (depth > 40 || PathComparison.IsRoot(directory))
        {
            return directory;
        }

        var key = PathComparison.Key(directory);
        if (_rules.RealDirectories.TryGetValue(key, out var cached))
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
            real = realParent is null || Same(PathComparison.Key(realParent), PathComparison.Key(parent!))
                ? directory
                : Path.Combine(realParent, PathComparison.Name(directory));
        }

        if (real is not null)
        {
            _rules.RealDirectories[key] = real;
        }

        return real;
    }

    /// <summary>Both arguments are keys.</summary>
    private bool Same(string a, string b) => a.Equals(b, _rules.Comparison);

    /// <inheritdoc cref="PathComparison.IsKeySameOrUnder"/>
    private bool Under(string path, string root) => PathComparison.IsKeySameOrUnder(path, root, _rules.Comparison);

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

    /// <summary>Everything the guard decides from, built once per run and shared by <see cref="Excluding"/>.</summary>
    /// <param name="SystemFolders">Keys of folders that must never be removed or contain a removed path.</param>
    /// <param name="StrictPathDirectories">PATH folders whose contents are protected too.</param>
    /// <param name="RealDirectories">Cache of each directory's link-resolved real path.</param>
    private sealed record Rules(
        StringComparison Comparison,
        string[] SystemFolders,
        string[] NonProjectFolders,
        string[] PathDirectories,
        string[] StrictPathDirectories,
        KeyedRoot[] Installs,
        Exclusion[] Exclusions,
        Lazy<string[]> Running,
        ConcurrentDictionary<string, string> RealDirectories);

    private sealed record KeyedRoot(string Key, string Path, string Reason, string[] CacheFolders);

    private sealed record Exclusion(string Key, string Path, string Source);
}

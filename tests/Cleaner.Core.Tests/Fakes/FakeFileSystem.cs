using System.Text.RegularExpressions;
using Cleaner.Core.Services;

namespace Cleaner.Core.Tests.Fakes;

public sealed class FakeFileSystem : IFileSystemService
{
    private static readonly char S = Path.DirectorySeparatorChar;
    private readonly Dictionary<string, long> _files = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _dirs = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<string>> _contents = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _links = new(StringComparer.OrdinalIgnoreCase);

    public HashSet<string> ThrowOnDelete { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Folders some program holds open: renaming them (or anything containing them) fails, as on Windows.</summary>
    public HashSet<string> InUse { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Path prefixes whose recursive delete throws, e.g. a staged folder that won't finish deleting.</summary>
    public List<string> FailDeletesUnder { get; } = [];

    /// <summary>A text file whose lines <see cref="ReadAllLines"/> returns.</summary>
    public FakeFileSystem AddFile(string path, string content)
    {
        AddFile(path, content.Length);
        _contents[Norm(path)] = [.. content.Split('\n').Select(l => l.TrimEnd('\r'))];
        return this;
    }

    /// <summary>A symlink or junction at <paramref name="path"/> pointing to <paramref name="target"/>.</summary>
    public FakeFileSystem AddLink(string path, string target)
    {
        AddDirectory(path);
        _links[Norm(path)] = Norm(target);
        return this;
    }

    public FakeFileSystem AddFile(string path, long size)
    {
        var p = Norm(path);
        _files[p] = size;
        AddAncestors(p);
        return this;
    }

    public FakeFileSystem AddDirectory(string path)
    {
        var p = Norm(path);
        _dirs.Add(p);
        AddAncestors(p);
        return this;
    }

    public bool DirectoryExists(string path)
    {
        var p = Norm(path);
        return !_files.ContainsKey(p) && (_dirs.Contains(p) || HasDescendant(p));
    }

    public bool FileExists(string path) => _files.ContainsKey(Norm(path));

    public long GetFileSize(string path) => _files.GetValueOrDefault(Norm(path));

    public long GetDirectorySize(string path)
    {
        var p = Norm(path);
        return _files.Where(kv => Under(kv.Key, p)).Sum(kv => kv.Value);
    }

    public IEnumerable<string> EnumerateDirectories(string path) =>
        EnumerateEntries(path).Where(e => !_files.ContainsKey(e) && !_links.ContainsKey(e));

    public IEnumerable<string> EnumerateEntries(string path)
    {
        var p = Norm(path);
        var children = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in _files.Keys.Concat(_dirs))
        {
            if (!Under(key, p))
            {
                continue;
            }

            children.Add(p + S + key[(p.Length + 1)..].Split(S)[0]);
        }

        return children;
    }

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*", bool recursive = false)
    {
        var p = Norm(path);
        foreach (var key in _files.Keys)
        {
            if (Under(key, p) && (recursive || ParentOf(key) == p) && MatchesPattern(key, searchPattern))
            {
                yield return key;
            }
        }
    }

    /// <summary>Matches the wildcard behavior of <c>Directory.EnumerateFiles</c>.</summary>
    private static bool MatchesPattern(string path, string searchPattern)
    {
        if (searchPattern is "*" or "*.*")
        {
            return true;
        }

        var expression = "^" + Regex.Escape(searchPattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$";
        return Regex.IsMatch(Path.GetFileName(path), expression, RegexOptions.IgnoreCase);
    }

    public void DeleteDirectory(string path)
    {
        var p = Norm(path);
        Guard(p);
        if (FailDeletesUnder.Any(prefix => p.StartsWith(Norm(prefix), StringComparison.OrdinalIgnoreCase)))
        {
            throw new IOException($"Could not finish deleting '{p}'.");
        }

        foreach (var key in _files.Keys.Where(k => k == p || Under(k, p)).ToList())
        {
            _files.Remove(key);
        }

        foreach (var dir in _dirs.Where(d => d == p || Under(d, p)).ToList())
        {
            _dirs.Remove(dir);
        }

        foreach (var link in _links.Keys.Where(l => l == p || Under(l, p)).ToList())
        {
            _links.Remove(link);
        }
    }

    public void DeleteFile(string path)
    {
        var p = Norm(path);
        Guard(p);
        _files.Remove(p);
    }

    public void WriteAllText(string path, string contents)
    {
        AddFile(path, contents);
    }

    public void AppendLine(string path, string line)
    {
        var p = Norm(path);
        var lines = _contents.TryGetValue(p, out var existing) ? existing : [];
        lines.Add(line);
        _contents[p] = lines;
        _files[p] = lines.Sum(l => l.Length + 1);
        AddAncestors(p);
    }

    public IReadOnlyList<string> ReadAllLines(string path) =>
        _contents.TryGetValue(Norm(path), out var lines) ? [.. lines] : [];

    public bool IsLink(string path) => _links.ContainsKey(Norm(path));

    public string? ResolveLinkTarget(string path) => _links.GetValueOrDefault(Norm(path));

    public void MoveDirectory(string source, string destination)
    {
        var from = Norm(source);
        var to = Norm(destination);
        Guard(from);
        if (InUse.Any(busy => Norm(busy) == from || Under(Norm(busy), from)))
        {
            throw new IOException($"The process cannot access '{from}' because it is being used by another process.");
        }

        string Moved(string key) => to + key[from.Length..];
        foreach (var key in _files.Keys.Where(k => Under(k, from)).ToList())
        {
            _files[Moved(key)] = _files[key];
            _files.Remove(key);
            if (_contents.Remove(key, out var lines))
            {
                _contents[Moved(key)] = lines;
            }
        }

        foreach (var dir in _dirs.Where(d => d == from || Under(d, from)).ToList())
        {
            _dirs.Remove(dir);
            _dirs.Add(Moved(dir));
        }

        foreach (var link in _links.Keys.Where(l => l == from || Under(l, from)).ToList())
        {
            _links[Moved(link)] = _links[link];
            _links.Remove(link);
        }

        AddAncestors(to);
    }

    private void Guard(string path)
    {
        if (ThrowOnDelete.Any(t => Norm(t) == path))
        {
            throw new UnauthorizedAccessException($"Access denied: {path}");
        }
    }

    private bool HasDescendant(string p) =>
        _files.Keys.Any(k => Under(k, p)) || _dirs.Any(d => Under(d, p));

    private void AddAncestors(string p)
    {
        var dir = ParentOf(p);
        while (!string.IsNullOrEmpty(dir) && _dirs.Add(dir))
        {
            dir = ParentOf(dir);
        }
    }

    private static bool Under(string key, string parent) =>
        key.StartsWith(parent + S, StringComparison.OrdinalIgnoreCase);

    private static string ParentOf(string p)
    {
        var i = p.LastIndexOf(S);
        return i <= 0 ? string.Empty : p[..i];
    }

    private static string Norm(string p) => p.Replace('/', S).Replace('\\', S).TrimEnd(S);
}

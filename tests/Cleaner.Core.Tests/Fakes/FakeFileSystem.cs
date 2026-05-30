using Cleaner.Core.Services;

namespace Cleaner.Core.Tests.Fakes;

/// <summary>An in-memory <see cref="IFileSystemService"/> for fast, side-effect-free tests.</summary>
public sealed class FakeFileSystem : IFileSystemService
{
    private static readonly char S = Path.DirectorySeparatorChar;
    private readonly Dictionary<string, long> _files = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _dirs = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Paths whose deletion should throw, to exercise error handling.</summary>
    public HashSet<string> ThrowOnDelete { get; } = new(StringComparer.OrdinalIgnoreCase);

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

    public IEnumerable<string> EnumerateDirectories(string path)
    {
        var p = Norm(path);
        var children = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var key in _files.Keys.Concat(_dirs))
        {
            if (!Under(key, p))
            {
                continue;
            }

            var child = p + S + key[(p.Length + 1)..].Split(S)[0];
            if (!_files.ContainsKey(child))
            {
                children.Add(child);
            }
        }

        return children;
    }

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*", bool recursive = false)
    {
        var p = Norm(path);
        foreach (var key in _files.Keys)
        {
            if (Under(key, p) && (recursive || ParentOf(key) == p))
            {
                yield return key;
            }
        }
    }

    public void DeleteDirectory(string path)
    {
        var p = Norm(path);
        Guard(p);
        foreach (var key in _files.Keys.Where(k => k == p || Under(k, p)).ToList())
        {
            _files.Remove(key);
        }

        foreach (var dir in _dirs.Where(d => d == p || Under(d, p)).ToList())
        {
            _dirs.Remove(dir);
        }
    }

    public void DeleteFile(string path)
    {
        var p = Norm(path);
        Guard(p);
        _files.Remove(p);
    }

    public void DeleteContents(string path)
    {
        var p = Norm(path);
        Guard(p);
        foreach (var key in _files.Keys.Where(k => Under(k, p)).ToList())
        {
            _files.Remove(key);
        }

        foreach (var dir in _dirs.Where(d => Under(d, p)).ToList())
        {
            _dirs.Remove(dir);
        }
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

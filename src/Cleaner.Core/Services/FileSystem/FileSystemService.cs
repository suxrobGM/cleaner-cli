namespace Cleaner.Core.Services;

/// <inheritdoc cref="IFileSystemService"/>
public sealed class FileSystemService : IFileSystemService
{
    public bool DirectoryExists(string path) => Directory.Exists(path);

    public bool FileExists(string path) => File.Exists(path);

    public long GetFileSize(string path)
    {
        try
        {
            return new FileInfo(path).Length;
        }
        catch
        {
            return 0;
        }
    }

    public long GetDirectorySize(string path)
    {
        if (!Directory.Exists(path))
        {
            return 0;
        }

        long total = 0;
        try
        {
            foreach (var file in Directory.EnumerateFiles(path, "*", RecursiveScan(recurse: true)))
            {
                try
                {
                    total += new FileInfo(file).Length;
                }
                catch
                {
                    // Skip files we can't stat.
                }
            }
        }
        catch
        {
            // Directory vanished or became inaccessible mid-scan.
        }

        return total;
    }

    public IEnumerable<string> EnumerateDirectories(string path)
    {
        if (!Directory.Exists(path))
        {
            return [];
        }

        try
        {
            // Skip links so a walk can't follow scoop's `current` or an nvm symlink into an install elsewhere.
            return Directory.EnumerateDirectories(path, "*", RecursiveScan(recurse: false));
        }
        catch
        {
            return [];
        }
    }

    public IEnumerable<string> EnumerateEntries(string path)
    {
        if (!Directory.Exists(path))
        {
            return [];
        }

        try
        {
            return Directory.EnumerateFileSystemEntries(path, "*", new EnumerationOptions
            {
                IgnoreInaccessible = true,
                AttributesToSkip = 0,
            });
        }
        catch
        {
            return [];
        }
    }

    public bool IsLink(string path) => LinkTargetOf(LinkInfo(path)) is not null;

    public string? ResolveLinkTarget(string path)
    {
        var info = LinkInfo(path);
        if (LinkTargetOf(info) is not { } target)
        {
            return null;
        }

        try
        {
            return info!.ResolveLinkTarget(returnFinalTarget: true)?.FullName ?? target;
        }
        catch
        {
            return target;
        }
    }

    public void MoveDirectory(string source, string destination) => Directory.Move(source, destination);

    public IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*", bool recursive = false)
    {
        if (!Directory.Exists(path))
        {
            return [];
        }

        try
        {
            return Directory.EnumerateFiles(path, searchPattern, RecursiveScan(recursive));
        }
        catch
        {
            return [];
        }
    }

    public void DeleteDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        try
        {
            Directory.Delete(path, recursive: true);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            // Read-only files (e.g. NuGet caches) block delete on Windows. Clearing them walks the
            // whole tree, so do it only after a failed delete.
            ClearReadOnlyAttributes(path);
            Directory.Delete(path, recursive: true);
        }
    }

    public void DeleteFile(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        TryClearReadOnly(path);
        File.Delete(path);
    }

    public void WriteAllText(string path, string contents)
    {
        var directory = Path.GetDirectoryName(path);
        if (directory is { Length: > 0 })
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, contents);
    }

    public IReadOnlyList<string> ReadAllLines(string path)
    {
        try
        {
            return File.Exists(path) ? File.ReadAllLines(path) : [];
        }
        catch
        {
            return [];
        }
    }

    public void AppendLine(string path, string line)
    {
        var directory = Path.GetDirectoryName(path);
        if (directory is { Length: > 0 })
        {
            Directory.CreateDirectory(directory);
        }

        File.AppendAllText(path, line + Environment.NewLine);
    }

    private static void ClearReadOnlyAttributes(string directory)
    {
        try
        {
            foreach (var file in Directory.EnumerateFiles(directory, "*", RecursiveScan(recurse: true)))
            {
                TryClearReadOnly(file);
            }
        }
        catch
        {
            // Best-effort.
        }
    }

    /// <summary>Skips inaccessible entries and never follows links, so a scan can't leave the target or loop.</summary>
    private static EnumerationOptions RecursiveScan(bool recurse) => new()
    {
        RecurseSubdirectories = recurse,
        IgnoreInaccessible = true,
        AttributesToSkip = FileAttributes.ReparsePoint,
    };

    /// <summary>The path as a directory or file, whichever it is; null when it can't be inspected.</summary>
    private static FileSystemInfo? LinkInfo(string path)
    {
        try
        {
            return Directory.Exists(path) ? new DirectoryInfo(path) : new FileInfo(path);
        }
        catch
        {
            return null;
        }
    }

    private static string? LinkTargetOf(FileSystemInfo? info)
    {
        try
        {
            return info?.LinkTarget;
        }
        catch
        {
            return null;
        }
    }

    private static void TryClearReadOnly(string file)
    {
        try
        {
            var attributes = File.GetAttributes(file);
            if ((attributes & FileAttributes.ReadOnly) != 0)
            {
                File.SetAttributes(file, attributes & ~FileAttributes.ReadOnly);
            }
        }
        catch
        {
            // Best-effort.
        }
    }
}

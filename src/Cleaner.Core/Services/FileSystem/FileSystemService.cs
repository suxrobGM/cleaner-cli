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
            return Directory.EnumerateDirectories(path);
        }
        catch
        {
            return [];
        }
    }

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
            // whole tree, so only do it on the rare delete that fails, then retry.
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

    public void DeleteContents(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        var errors = new List<Exception>();

        foreach (var dir in Directory.EnumerateDirectories(path))
        {
            try
            {
                DeleteDirectory(dir);
            }
            catch (Exception ex)
            {
                errors.Add(ex);
            }
        }

        foreach (var file in Directory.EnumerateFiles(path))
        {
            try
            {
                DeleteFile(file);
            }
            catch (Exception ex)
            {
                errors.Add(ex);
            }
        }

        if (errors.Count > 0)
        {
            throw new AggregateException($"Failed to delete some contents of '{path}'.", errors);
        }
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

    /// <summary>
    /// Shared enumeration options: skip inaccessible entries and never follow reparse points
    /// (symlinks/junctions), so a scan can't wander outside the target or loop.
    /// </summary>
    private static EnumerationOptions RecursiveScan(bool recurse) => new()
    {
        RecurseSubdirectories = recurse,
        IgnoreInaccessible = true,
        AttributesToSkip = FileAttributes.ReparsePoint,
    };

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

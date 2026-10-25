namespace Cleaner.Core.Services;

/// <summary>Filesystem abstraction with best-effort enumeration and sizing.</summary>
public interface IFileSystemService
{
    bool DirectoryExists(string path);

    bool FileExists(string path);

    /// <summary>Recursive size in bytes; unreadable entries are skipped.</summary>
    long GetDirectorySize(string path);

    long GetFileSize(string path);

    /// <summary>Child directories, excluding symlinks and junctions so a walk never leaves its root.</summary>
    IEnumerable<string> EnumerateDirectories(string path);

    IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*", bool recursive = false);

    /// <summary>Every direct child, files and directories, links included.</summary>
    IEnumerable<string> EnumerateEntries(string path);

    /// <summary>True when the path itself is a symlink, junction, or other reparse point.</summary>
    bool IsLink(string path);

    /// <summary>Where a link finally points, or null when the path is not a link.</summary>
    string? ResolveLinkTarget(string path);

    /// <summary>Atomic on one volume; on Windows it fails while anything inside is open.</summary>
    void MoveDirectory(string source, string destination);

    void DeleteDirectory(string path);

    void DeleteFile(string path);

    /// <summary>Empty when the file can't be read.</summary>
    IReadOnlyList<string> ReadAllLines(string path);

    void WriteAllText(string path, string contents);

    /// <summary>Creates the file and its directory if needed.</summary>
    void AppendLine(string path, string line);
}

namespace Cleaner.Core.Services;

/// <summary>
/// All filesystem access goes through this abstraction so cleaners can be unit-tested against an
/// in-memory fake. Implementations are best-effort: enumeration and sizing skip entries that throw
/// (e.g. access denied) rather than failing the whole operation.
/// </summary>
public interface IFileSystemService
{
    bool DirectoryExists(string path);

    bool FileExists(string path);

    /// <summary>Recursive total size of a directory in bytes; unreadable entries are skipped.</summary>
    long GetDirectorySize(string path);

    long GetFileSize(string path);

    IEnumerable<string> EnumerateDirectories(string path);

    IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*", bool recursive = false);

    /// <summary>Delete a directory and everything under it.</summary>
    void DeleteDirectory(string path);

    void DeleteFile(string path);

    /// <summary>Delete everything inside a directory but keep the directory itself.</summary>
    void DeleteContents(string path);
}

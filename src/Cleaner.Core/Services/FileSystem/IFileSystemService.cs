namespace Cleaner.Core.Services;

/// <summary>
/// All filesystem access, so cleaners can be tested against an in-memory fake. Best-effort:
/// enumeration and sizing skip entries that throw rather than failing the whole operation.
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

    /// <summary>Write text to <paramref name="path"/>, creating or overwriting it.</summary>
    void WriteAllText(string path, string contents);

    /// <summary>Delete everything inside a directory but keep the directory itself.</summary>
    void DeleteContents(string path);
}

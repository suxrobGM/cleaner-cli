namespace Cleaner.Core.Services;

public enum OsPlatform
{
    Unknown = 0,
    Windows,
    MacOs,
    Linux,
}

/// <summary>Provides OS identity, paths, and elevation without direct platform dependencies.</summary>
public interface IEnvironmentService
{
    OsPlatform Os { get; }

    bool IsWindows { get; }

    bool IsMacOs { get; }

    bool IsLinux { get; }

    /// <summary>True when running as Windows administrator or Unix root.</summary>
    bool IsElevated { get; }

    string HomeDirectory { get; }

    string TempDirectory { get; }

    /// <summary>%LOCALAPPDATA% on Windows.</summary>
    string LocalAppDataDirectory { get; }

    /// <summary>%APPDATA% on Windows.</summary>
    string AppDataDirectory { get; }

    /// <summary>Conventional cache root for the current platform.</summary>
    string CacheDirectory { get; }

    /// <summary>%SystemRoot%, or null on non-Windows.</summary>
    string? WindowsDirectory { get; }

    /// <summary><c>~/.cleaner</c>: Cleaner's own logs, exclude file, and unfinished-delete list.</summary>
    string DataDirectory { get; }

    /// <summary><c>~/.cleaner/logs</c>.</summary>
    string LogDirectory { get; }

    /// <summary>
    /// Expanded, absolute, de-duplicated PATH entries. On Windows this adds the user and machine PATH,
    /// so a tool installed since the terminal opened is still found.
    /// </summary>
    IReadOnlyList<string> PathDirectories { get; }

    /// <summary>Roots of the ready, fixed (non-removable) drives, e.g. <c>C:\</c>.</summary>
    IReadOnlyList<string> FixedDriveRoots { get; }

    string? GetEnvironmentVariable(string name);

    string HomePath(params string[] segments);
}

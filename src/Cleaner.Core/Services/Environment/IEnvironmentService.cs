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

    /// <summary>True if the process is running elevated (Windows administrator / Unix root).</summary>
    bool IsElevated { get; }

    /// <summary>The current user's home directory.</summary>
    string HomeDirectory { get; }

    /// <summary>The system temp directory.</summary>
    string TempDirectory { get; }

    /// <summary>Per-user local (non-roaming) application data: %LOCALAPPDATA% on Windows.</summary>
    string LocalAppDataDirectory { get; }

    /// <summary>Per-user roaming application data: %APPDATA% on Windows.</summary>
    string AppDataDirectory { get; }

    /// <summary>Conventional cache root for the current platform.</summary>
    string CacheDirectory { get; }

    /// <summary>The Windows directory (%SystemRoot%), or null on non-Windows.</summary>
    string? WindowsDirectory { get; }

    /// <summary>Directory where Cleaner writes its own log files: <c>~/.cleaner/logs</c>.</summary>
    string LogDirectory { get; }

    string? GetEnvironmentVariable(string name);

    /// <summary>Combine path segments under the user's home directory.</summary>
    string HomePath(params string[] segments);
}

namespace Cleaner.Core.Services;

public enum OsPlatform
{
    Unknown = 0,
    Windows,
    MacOs,
    Linux,
}

/// <summary>
/// The single place operating-system differences live. Cleaners ask this service for paths,
/// OS identity, and elevation rather than touching <see cref="System.Environment"/> directly,
/// which keeps them cross-platform and testable.
/// </summary>
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

    /// <summary>
    /// Conventional cache root: %LOCALAPPDATA% on Windows, ~/Library/Caches on macOS,
    /// $XDG_CACHE_HOME or ~/.cache on Linux.
    /// </summary>
    string CacheDirectory { get; }

    /// <summary>The Windows directory (%SystemRoot%), or null on non-Windows.</summary>
    string? WindowsDirectory { get; }

    string? GetEnvironmentVariable(string name);

    /// <summary>Combine path segments under the user's home directory.</summary>
    string HomePath(params string[] segments);
}

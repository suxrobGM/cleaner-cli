using Cleaner.Core.Services;

namespace Cleaner.Core.Cleaners.Base;

/// <summary>
/// Helpers for the common "per-OS application cache" path shapes, so cleaners don't repeat the
/// Windows/macOS/Linux branching.
/// </summary>
internal static class OsPaths
{
    /// <summary>
    /// Resolve an application's cache directory following the usual conventions:
    /// <c>%LOCALAPPDATA%\{windows}</c>, <c>~/Library/Caches/{macOs}</c>, or
    /// <c>$XDG_CACHE_HOME/{linux}</c> (i.e. <c>~/.cache/{linux}</c>).
    /// </summary>
    public static string AppCache(IEnvironmentService env, string windows, string macOs, string linux)
    {
        if (env.IsWindows)
        {
            return Path.Combine(env.LocalAppDataDirectory, windows);
        }

        return env.IsMacOs
            ? Path.Combine(env.HomeDirectory, "Library", "Caches", macOs)
            : Path.Combine(env.CacheDirectory, linux);
    }

    /// <summary>
    /// Combine <paramref name="segments"/> onto the drive root of a Windows path (e.g.
    /// <c>C:\Windows</c> → <c>C:\NVIDIA</c>). Deliberately avoids <see cref="Path.GetPathRoot"/> and
    /// <see cref="Path.Combine"/> for the root: both only recognise <c>C:\</c> as rooted on Windows
    /// hosts, so on Linux/macOS (CI, cross-platform builds) they mangle these paths. Joins with an
    /// explicit backslash so the result is a valid Windows path regardless of the host OS.
    /// </summary>
    public static string FromWindowsDriveRoot(string windowsPath, params string[] segments)
    {
        var drive = windowsPath.Length >= 2 && windowsPath[1] == ':' ? windowsPath[..2] : "C:";
        return string.Join('\\', segments.Prepend(drive));
    }

    /// <summary>
    /// The value of the first set (non-blank) environment variable among <paramref name="names"/>,
    /// or null. Used for cache-relocation overrides like <c>NUGET_PACKAGES</c> or <c>CARGO_HOME</c>.
    /// </summary>
    public static string? Env(IEnvironmentService env, params string[] names)
    {
        foreach (var name in names)
        {
            var value = env.GetEnvironmentVariable(name);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}

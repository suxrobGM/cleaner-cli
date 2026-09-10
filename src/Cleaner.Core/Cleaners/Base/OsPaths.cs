using Cleaner.Core.Services;

namespace Cleaner.Core.Cleaners.Base;

/// <summary>Per-OS cache path shapes, so cleaners don't repeat the platform branching.</summary>
internal static class OsPaths
{
    /// <summary>
    /// An app's cache directory: <c>%LOCALAPPDATA%\{windows}</c>, <c>~/Library/Caches/{macOs}</c>,
    /// or <c>$XDG_CACHE_HOME/{linux}</c>.
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
    /// Combine <paramref name="segments"/> onto a Windows path's drive root (<c>C:\Windows</c> →
    /// <c>C:\NVIDIA</c>). Hand-joined rather than via <see cref="Path"/>, which only treats
    /// <c>C:\</c> as rooted on Windows hosts and mangles these paths on a Linux/macOS build.
    /// </summary>
    public static string FromWindowsDriveRoot(string windowsPath, params string[] segments)
    {
        var drive = windowsPath.Length >= 2 && windowsPath[1] == ':' ? windowsPath[..2] : "C:";
        return string.Join('\\', segments.Prepend(drive));
    }

    /// <summary>
    /// First non-blank variable among <paramref name="names"/>, or null. For cache-relocation
    /// overrides like <c>NUGET_PACKAGES</c>.
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

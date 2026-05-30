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
}

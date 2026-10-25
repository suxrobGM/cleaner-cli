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
    /// An app's per-user data directory: <c>%APPDATA%\{windows}</c>,
    /// <c>~/Library/Application Support/{macOs}</c>, or <c>~/.config/{linux}</c>.
    /// </summary>
    public static string AppData(IEnvironmentService env, string windows, string macOs, string linux)
    {
        if (env.IsWindows)
        {
            return Path.Combine(env.AppDataDirectory, windows);
        }

        return env.IsMacOs
            ? Path.Combine(env.HomeDirectory, "Library", "Application Support", macOs)
            : env.HomePath(".config", linux);
    }

    /// <summary>The per-user data root itself, for callers that append their own segments.</summary>
    public static string AppDataRoot(IEnvironmentService env)
    {
        if (env.IsWindows)
        {
            return env.AppDataDirectory;
        }

        return env.IsMacOs
            ? Path.Combine(env.HomeDirectory, "Library", "Application Support")
            : env.HomePath(".config");
    }

    /// <summary>
    /// A path under <c>%ProgramData%</c>, falling back to the Windows drive when the variable is unset;
    /// null when neither resolves.
    /// </summary>
    public static string? ProgramData(IEnvironmentService env, params string[] segments)
    {
        var root = Env(env, "ProgramData")
            ?? (env.WindowsDirectory is { } windows ? FromWindowsDriveRoot(windows, "ProgramData") : null);

        return root is null ? null : Path.Combine([root, .. segments]);
    }

    /// <summary>
    /// A path under <c>%ProgramFiles%</c> (or its x86 sibling), falling back to the Windows drive; null when
    /// neither resolves.
    /// </summary>
    public static string? ProgramFiles(IEnvironmentService env, bool x86 = false, params string[] segments)
    {
        var directory = x86 ? "Program Files (x86)" : "Program Files";
        var root = Env(env, x86 ? "ProgramFiles(x86)" : "ProgramFiles")
            ?? (env.WindowsDirectory is { } windows ? FromWindowsDriveRoot(windows, directory) : null);

        return root is null ? null : Path.Combine([root, .. segments]);
    }

    /// <summary>
    /// Joins <paramref name="segments"/> onto a Windows path's drive (<c>C:\Windows</c> → <c>C:\NVIDIA</c>).
    /// Joined by hand because <see cref="Path"/> treats <c>C:\</c> as rooted only on Windows hosts.
    /// </summary>
    public static string FromWindowsDriveRoot(string windowsPath, params string[] segments)
    {
        var drive = windowsPath.Length >= 2 && windowsPath[1] == ':' ? windowsPath[..2] : "C:";
        return string.Join('\\', segments.Prepend(drive));
    }

    /// <summary>
    /// First non-blank variable among <paramref name="names"/>, or null; used for cache-location overrides
    /// like <c>NUGET_PACKAGES</c>.
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

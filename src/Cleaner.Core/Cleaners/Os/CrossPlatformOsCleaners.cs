using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.Os;

/// <summary>Clears the per-user temporary directory.</summary>
public sealed class UserTempCleaner : DirectoryCleanerBase
{
    public override string Id => "temp";

    public override string Name => "User temp files";

    public override string Category => Categories.OperatingSystem;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        [new CleanupPath(context.Environment.TempDirectory, DeleteMode.ClearContents, "temp directory")];
}

/// <summary>Empties the recycle bin / trash for the current user.</summary>
public sealed class TrashCleaner : DirectoryCleanerBase
{
    public override string Id => "trash";

    public override string Name => "Recycle Bin / Trash";

    public override string Category => Categories.OperatingSystem;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;

        if (env.IsWindows)
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive is { DriveType: DriveType.Fixed, IsReady: true })
                {
                    yield return new CleanupPath(
                        Path.Combine(drive.RootDirectory.FullName, "$Recycle.Bin"),
                        DeleteMode.ClearContents,
                        drive.Name);
                }
            }

            yield break;
        }

        if (env.IsMacOs)
        {
            yield return new CleanupPath(Path.Combine(env.HomeDirectory, ".Trash"), DeleteMode.ClearContents);
            yield break;
        }

        // Linux / freedesktop trash spec.
        var dataHome = env.GetEnvironmentVariable("XDG_DATA_HOME");
        var trashRoot = !string.IsNullOrWhiteSpace(dataHome)
            ? Path.Combine(dataHome, "Trash")
            : Path.Combine(env.HomeDirectory, ".local", "share", "Trash");
        yield return new CleanupPath(Path.Combine(trashRoot, "files"), DeleteMode.ClearContents, "files");
        yield return new CleanupPath(Path.Combine(trashRoot, "info"), DeleteMode.ClearContents, "info");
    }
}

/// <summary>Clears Chrome, Edge, and Firefox HTTP/code caches (keeps history, cookies, profiles).</summary>
public sealed class BrowserCacheCleaner : DirectoryCleanerBase
{
    private static readonly string[] ChromiumProfileCaches = ["Cache", "Code Cache", "GPUCache"];

    public override string Id => "browser-cache";

    public override string Name => "Browser caches (Chrome/Edge/Firefox)";

    public override string Category => Categories.OperatingSystem;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;

        if (env.IsWindows)
        {
            var local = env.LocalAppDataDirectory;
            foreach (var path in ChromiumProfiles(context, Path.Combine(local, "Google", "Chrome", "User Data"), "Chrome"))
            {
                yield return path;
            }

            foreach (var path in ChromiumProfiles(context, Path.Combine(local, "Microsoft", "Edge", "User Data"), "Edge"))
            {
                yield return path;
            }

            foreach (var profile in context.FileSystem.EnumerateDirectories(Path.Combine(local, "Mozilla", "Firefox", "Profiles")))
            {
                yield return new CleanupPath(Path.Combine(profile, "cache2"), DeleteMode.ClearContents, "Firefox");
            }

            yield break;
        }

        if (env.IsMacOs)
        {
            var caches = Path.Combine(env.HomeDirectory, "Library", "Caches");
            yield return new CleanupPath(Path.Combine(caches, "Google", "Chrome"), DeleteMode.ClearContents, "Chrome");
            yield return new CleanupPath(Path.Combine(caches, "Microsoft Edge"), DeleteMode.ClearContents, "Edge");
            yield return new CleanupPath(Path.Combine(caches, "Firefox"), DeleteMode.ClearContents, "Firefox");
            yield break;
        }

        var cache = env.CacheDirectory;
        yield return new CleanupPath(Path.Combine(cache, "google-chrome"), DeleteMode.ClearContents, "Chrome");
        yield return new CleanupPath(Path.Combine(cache, "microsoft-edge"), DeleteMode.ClearContents, "Edge");
        yield return new CleanupPath(Path.Combine(cache, "mozilla", "firefox"), DeleteMode.ClearContents, "Firefox");
    }

    private static IEnumerable<CleanupPath> ChromiumProfiles(CleanupContext context, string userDataRoot, string browser)
    {
        foreach (var profile in context.FileSystem.EnumerateDirectories(userDataRoot))
        {
            foreach (var sub in ChromiumProfileCaches)
            {
                yield return new CleanupPath(Path.Combine(profile, sub), DeleteMode.ClearContents, browser);
            }
        }
    }
}

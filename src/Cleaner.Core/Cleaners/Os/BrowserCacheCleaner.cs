using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.Os;

/// <summary>
/// Clears the HTTP/code caches of installed browsers — Chrome, Edge, Brave, Opera, Vivaldi,
/// Chromium, Arc, and Firefox — keeping history, cookies, and profiles.
/// </summary>
public sealed class BrowserCacheCleaner : DirectoryCleanerBase
{
    /// <summary>Chromium-based browsers on Windows: label + User Data root relative to %LOCALAPPDATA%.</summary>
    private static readonly (string Label, string[] Segments)[] WindowsChromiumBrowsers =
    [
        ("Chrome", ["Google", "Chrome", "User Data"]),
        ("Edge", ["Microsoft", "Edge", "User Data"]),
        ("Brave", ["BraveSoftware", "Brave-Browser", "User Data"]),
        ("Opera", ["Opera Software", "Opera Stable"]),
        ("Vivaldi", ["Vivaldi", "User Data"]),
        ("Chromium", ["Chromium", "User Data"]),
        ("Arc", ["Arc", "User Data"]),
    ];

    public override string Id => "browser-cache";

    public override string Name => "Browser caches (Chrome/Edge/Brave/Firefox/…)";

    public override string Category => Categories.OperatingSystem;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;

        if (env.IsWindows)
        {
            var local = env.LocalAppDataDirectory;
            foreach (var (label, segments) in WindowsChromiumBrowsers)
            {
                foreach (var path in ChromiumProfiles(context, Path.Combine([local, .. segments]), label))
                {
                    yield return path;
                }
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
            yield return new CleanupPath(Path.Combine(caches, "BraveSoftware", "Brave-Browser"), DeleteMode.ClearContents, "Brave");
            yield return new CleanupPath(Path.Combine(caches, "com.operasoftware.Opera"), DeleteMode.ClearContents, "Opera");
            yield return new CleanupPath(Path.Combine(caches, "Vivaldi"), DeleteMode.ClearContents, "Vivaldi");
            yield return new CleanupPath(Path.Combine(caches, "Chromium"), DeleteMode.ClearContents, "Chromium");
            yield return new CleanupPath(Path.Combine(caches, "Arc"), DeleteMode.ClearContents, "Arc");
            yield return new CleanupPath(Path.Combine(caches, "Firefox"), DeleteMode.ClearContents, "Firefox");
            yield break;
        }

        var cache = env.CacheDirectory;
        yield return new CleanupPath(Path.Combine(cache, "google-chrome"), DeleteMode.ClearContents, "Chrome");
        yield return new CleanupPath(Path.Combine(cache, "microsoft-edge"), DeleteMode.ClearContents, "Edge");
        yield return new CleanupPath(Path.Combine(cache, "BraveSoftware", "Brave-Browser"), DeleteMode.ClearContents, "Brave");
        yield return new CleanupPath(Path.Combine(cache, "opera"), DeleteMode.ClearContents, "Opera");
        yield return new CleanupPath(Path.Combine(cache, "vivaldi"), DeleteMode.ClearContents, "Vivaldi");
        yield return new CleanupPath(Path.Combine(cache, "chromium"), DeleteMode.ClearContents, "Chromium");
        yield return new CleanupPath(Path.Combine(cache, "mozilla", "firefox"), DeleteMode.ClearContents, "Firefox");
    }

    private static IEnumerable<CleanupPath> ChromiumProfiles(CleanupContext context, string userDataRoot, string browser)
    {
        // Some browsers (e.g. Opera) keep the cache dirs directly in the root instead of per-profile.
        foreach (var path in ChromiumCache.Under(userDataRoot, browser))
        {
            yield return path;
        }

        foreach (var profile in context.FileSystem.EnumerateDirectories(userDataRoot))
        {
            foreach (var path in ChromiumCache.Under(profile, browser))
            {
                yield return path;
            }
        }
    }
}

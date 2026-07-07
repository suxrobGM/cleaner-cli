using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.Applications;

/// <summary>
/// Web/browser caches of game launchers — Epic Games, Battle.net, GOG Galaxy, EA app, and the Riot
/// Client. Game installs, saves, and login state are never touched. Steam has its own cleaner.
/// </summary>
public sealed class GameLauncherCleaner : DirectoryCleanerBase
{
    public override string Id => "game-launchers";

    public override string Name => "Game launcher caches (Epic/Battle.net/GOG/EA/Riot)";

    public override string Category => Categories.Applications;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;

        if (env.IsWindows)
        {
            var local = env.LocalAppDataDirectory;
            yield return new CleanupPath(
                Path.Combine(local, "EpicGamesLauncher", "Saved", "webcache"), DeleteMode.ClearContents, "Epic Games");

            // The launcher versions its webcache dirs (webcache_4430, ...); cover those too.
            var epicSaved = Path.Combine(local, "EpicGamesLauncher", "Saved");
            foreach (var dir in context.FileSystem.EnumerateDirectories(epicSaved))
            {
                if (DirectorySweep.LeafName(dir).StartsWith("webcache", StringComparison.OrdinalIgnoreCase))
                {
                    yield return new CleanupPath(dir, DeleteMode.ClearContents, "Epic Games");
                }
            }

            yield return new CleanupPath(Path.Combine(local, "Battle.net", "BrowserCache"), DeleteMode.ClearContents, "Battle.net");
            yield return new CleanupPath(Path.Combine(local, "Battle.net", "Cache"), DeleteMode.ClearContents, "Battle.net");
            yield return new CleanupPath(Path.Combine(local, "GOG.com", "Galaxy", "webcache"), DeleteMode.ClearContents, "GOG Galaxy");
            yield return new CleanupPath(Path.Combine(local, "Electronic Arts", "EA Desktop", "cache"), DeleteMode.ClearContents, "EA app");
            yield return new CleanupPath(Path.Combine(local, "Riot Games", "Riot Client", "Cache"), DeleteMode.ClearContents, "Riot Client");
            yield return new CleanupPath(Path.Combine(local, "Riot Games", "Riot Client", "Logs"), DeleteMode.ClearContents, "Riot Client logs");
            yield break;
        }

        if (env.IsMacOs)
        {
            var caches = Path.Combine(env.HomeDirectory, "Library", "Caches");
            yield return new CleanupPath(Path.Combine(caches, "com.epicgames.EpicGamesLauncher"), DeleteMode.ClearContents, "Epic Games");
            yield return new CleanupPath(Path.Combine(caches, "Battle.net"), DeleteMode.ClearContents, "Battle.net");
            yield return new CleanupPath(Path.Combine(caches, "GOG.com"), DeleteMode.ClearContents, "GOG Galaxy");
        }
    }
}

/// <summary>Adobe shared media caches (Premiere/After Effects render and peak files; regenerated).</summary>
public sealed class AdobeMediaCacheCleaner : DirectoryCleanerBase
{
    public override string Id => "adobe-media-cache";

    public override string Name => "Adobe media cache";

    public override string Category => Categories.Applications;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        var common = env.IsWindows
            ? Path.Combine(env.AppDataDirectory, "Adobe", "Common")
            : Path.Combine(env.HomeDirectory, "Library", "Application Support", "Adobe", "Common");

        yield return new CleanupPath(Path.Combine(common, "Media Cache Files"), DeleteMode.ClearContents, "media cache files");
        yield return new CleanupPath(Path.Combine(common, "Media Cache"), DeleteMode.ClearContents, "media cache database");
        yield return new CleanupPath(Path.Combine(common, "Peak Files"), DeleteMode.ClearContents, "audio peak files");
    }
}

/// <summary>OneDrive client logs and setup logs; synced content is never touched.</summary>
public sealed class OneDriveCleaner : DirectoryCleanerBase
{
    public override string Id => "onedrive";

    public override string Name => "OneDrive logs";

    public override string Category => Categories.Applications;

    public override bool IsApplicable(CleanupContext context) => context.Environment.IsWindows;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var root = Path.Combine(context.Environment.LocalAppDataDirectory, "Microsoft", "OneDrive");
        yield return new CleanupPath(Path.Combine(root, "logs"), DeleteMode.ClearContents, "client logs");
        yield return new CleanupPath(Path.Combine(root, "setup", "logs"), DeleteMode.ClearContents, "setup logs");
    }
}

/// <summary>
/// Dropbox's internal <c>.dropbox.cache</c> staging folder (officially safe to purge; synced files
/// are never touched). Only the default <c>~/Dropbox</c> location is covered.
/// </summary>
public sealed class DropboxCleaner : DirectoryCleanerBase
{
    public override string Id => "dropbox";

    public override string Name => "Dropbox internal cache";

    public override string Category => Categories.Applications;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        [new CleanupPath(context.Environment.HomePath("Dropbox", ".dropbox.cache"), DeleteMode.ClearContents)];
}

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

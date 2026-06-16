using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.Applications;

/// <summary>
/// Spotify's offline media/data caches (<c>Storage</c>/<c>Data</c>), re-filled on demand. Keeps
/// settings, login state, and local files.
/// </summary>
public sealed class SpotifyCleaner : DirectoryCleanerBase
{
    public override string Id => "spotify";

    public override string Name => "Spotify cache";

    public override string Category => Categories.Applications;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;

        if (env.IsWindows)
        {
            var root = Path.Combine(env.LocalAppDataDirectory, "Spotify");
            yield return new CleanupPath(Path.Combine(root, "Storage"), DeleteMode.ClearContents, "media cache");
            yield return new CleanupPath(Path.Combine(root, "Data"), DeleteMode.ClearContents, "data cache");
            yield break;
        }

        if (env.IsMacOs)
        {
            yield return new CleanupPath(
                Path.Combine(env.HomeDirectory, "Library", "Caches", "com.spotify.client"),
                DeleteMode.ClearContents, "media cache");
            yield break;
        }

        yield return new CleanupPath(Path.Combine(env.CacheDirectory, "spotify"), DeleteMode.ClearContents, "media cache");
    }
}

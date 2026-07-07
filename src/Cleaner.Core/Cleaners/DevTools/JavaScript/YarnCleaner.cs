using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>Yarn cache. Prefers <c>yarn cache clean</c>.</summary>
public sealed class YarnCleaner : ProcessCleanerBase
{
    public override string Id => "yarn";

    public override string Name => "Yarn cache";

    public override string Category => Categories.JavaScript;

    protected override string Executable => "yarn";

    protected override IReadOnlyList<string> CleanArguments => ["cache", "clean"];

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        if (OsPaths.Env(env, "YARN_CACHE_FOLDER") is { } configured)
        {
            yield return new CleanupPath(configured);
            yield break;
        }

        if (env.IsWindows)
        {
            yield return new CleanupPath(Path.Combine(env.LocalAppDataDirectory, "Yarn", "Cache"));
        }
        else
        {
            yield return new CleanupPath(Path.Combine(env.CacheDirectory, "yarn"));
            if (env.IsMacOs)
            {
                // Yarn Classic uses a capital Y; matters on case-sensitive APFS volumes.
                yield return new CleanupPath(Path.Combine(env.HomeDirectory, "Library", "Caches", "Yarn"));
            }
        }

        // Yarn Berry keeps its global mirror separately from the Classic cache.
        yield return new CleanupPath(env.HomePath(".yarn", "berry", "cache"), Description: "Berry global cache");
    }
}

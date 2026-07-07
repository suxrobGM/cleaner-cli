using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>Zig global compilation cache.</summary>
public sealed class ZigCleaner : DirectoryCleanerBase
{
    public override string Id => "zig";

    public override string Name => "Zig global cache";

    public override string Category => Categories.Languages;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        if (OsPaths.Env(env, "ZIG_GLOBAL_CACHE_DIR") is { } configured)
        {
            yield return new CleanupPath(configured);
            yield break;
        }

        yield return new CleanupPath(OsPaths.AppCache(env, "zig", "zig", "zig"));

        // zig also uses ~/.cache/zig on macOS in several versions; harmless if absent.
        yield return new CleanupPath(env.HomePath(".cache", "zig"));
    }
}

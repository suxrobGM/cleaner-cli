using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>Bun install cache (covers bunx).</summary>
public sealed class BunCleaner : DirectoryCleanerBase
{
    public override string Id => "bun";

    public override string Name => "Bun cache";

    public override string Category => Categories.JavaScript;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        var cache = OsPaths.Env(env, "BUN_INSTALL_CACHE_DIR");
        if (cache is null)
        {
            var install = OsPaths.Env(env, "BUN_INSTALL") ?? env.HomePath(".bun");
            cache = Path.Combine(install, "install", "cache");
        }

        yield return new CleanupPath(cache);
    }
}

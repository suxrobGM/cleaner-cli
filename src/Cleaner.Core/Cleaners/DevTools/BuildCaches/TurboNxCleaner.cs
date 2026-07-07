using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>Turborepo and Nx remote/local task caches (global and project-local).</summary>
public sealed class TurboNxCleaner : DirectoryCleanerBase
{
    public override string Id => "turbo-nx";

    public override string Name => "Turbo / Nx cache";

    public override string Category => Categories.BuildCaches;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return new CleanupPath(Path.Combine(env.CacheDirectory, "turbo"), Description: "global turbo cache");
        yield return new CleanupPath(Path.Combine(env.CacheDirectory, "nx"), Description: "global nx cache");
        yield return new CleanupPath(Path.Combine(context.WorkingDirectory, ".turbo"), Description: "project .turbo");
        yield return new CleanupPath(Path.Combine(context.WorkingDirectory, ".nx", "cache"), Description: "project .nx/cache");
    }
}

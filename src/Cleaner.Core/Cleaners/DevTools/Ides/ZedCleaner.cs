using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>Zed editor cache.</summary>
public sealed class ZedCleaner : DirectoryCleanerBase
{
    public override string Id => "zed";

    public override string Name => "Zed cache";

    public override string Category => Categories.Ides;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return env.IsMacOs
            ? new CleanupPath(Path.Combine(env.HomeDirectory, "Library", "Caches", "dev.zed.Zed"), DeleteMode.ClearContents)
            : new CleanupPath(OsPaths.AppCache(env, Path.Combine("Zed", "cache"), "Zed", "zed"), DeleteMode.ClearContents);
    }
}

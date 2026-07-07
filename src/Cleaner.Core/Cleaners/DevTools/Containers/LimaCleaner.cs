using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>Lima VM image download cache (used by lima and colima on macOS/Linux).</summary>
public sealed class LimaCleaner : DirectoryCleanerBase
{
    public override string Id => "lima";

    public override string Name => "Lima image cache";

    public override string Category => Categories.Containers;

    public override bool IsApplicable(CleanupContext context) => !context.Environment.IsWindows;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return env.IsMacOs
            ? new CleanupPath(Path.Combine(env.HomeDirectory, "Library", "Caches", "lima"))
            : new CleanupPath(Path.Combine(env.CacheDirectory, "lima"));
    }
}

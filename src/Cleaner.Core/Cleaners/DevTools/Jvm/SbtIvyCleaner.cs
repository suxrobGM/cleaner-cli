using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>sbt / Ivy resolution caches.</summary>
public sealed class SbtIvyCleaner : DirectoryCleanerBase
{
    public override string Id => "sbt";

    public override string Name => "sbt / Ivy cache";

    public override string Category => Categories.Jvm;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return new CleanupPath(env.HomePath(".ivy2", "cache"), Description: "Ivy cache");
        yield return new CleanupPath(env.HomePath(".sbt", "boot"), Description: "sbt boot");
    }
}

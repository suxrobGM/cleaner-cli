using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>ccache compiler cache.</summary>
public sealed class CcacheCleaner : DirectoryCleanerBase
{
    public override string Id => "ccache";

    public override string Name => "ccache";

    public override string Category => Categories.BuildCaches;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return new CleanupPath(env.HomePath(".ccache"), DeleteMode.ClearContents);
        yield return new CleanupPath(Path.Combine(env.CacheDirectory, "ccache"), DeleteMode.ClearContents);
    }
}

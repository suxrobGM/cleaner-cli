using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>pip download/wheel cache. Prefers <c>pip cache purge</c>.</summary>
public sealed class PipCleaner : ProcessCleanerBase
{
    public override string Id => "pip";

    public override string Name => "pip cache";

    public override string Category => Categories.Python;

    protected override string Executable => "pip";

    protected override IReadOnlyList<string> CleanArguments => ["cache", "purge"];

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return new CleanupPath(
            OsPaths.Env(env, "PIP_CACHE_DIR") ?? OsPaths.AppCache(env, Path.Combine("pip", "Cache"), "pip", "pip"));
    }
}

using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>Poetry artifact and download cache.</summary>
public sealed class PoetryCleaner : DirectoryCleanerBase
{
    public override string Id => "poetry";

    public override string Name => "Poetry cache";

    public override string Category => Categories.Python;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return new CleanupPath(
            OsPaths.Env(env, "POETRY_CACHE_DIR")
            ?? OsPaths.AppCache(env, Path.Combine("pypoetry", "Cache"), "pypoetry", "pypoetry"));
    }
}

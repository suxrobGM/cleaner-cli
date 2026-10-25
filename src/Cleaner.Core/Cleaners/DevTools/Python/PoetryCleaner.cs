using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// Poetry artifact and download cache. <c>virtualenvs</c> under the same root is kept: those are the
/// projects' environments, not cache.
/// </summary>
public sealed class PoetryCleaner : DirectoryCleanerBase
{
    public override string Id => "poetry";

    public override string Name => "Poetry cache";

    public override string Category => Categories.Python;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        var root = OsPaths.Env(env, "POETRY_CACHE_DIR")
            ?? OsPaths.AppCache(env, Path.Combine("pypoetry", "Cache"), "pypoetry", "pypoetry");
        yield return new CleanupPath(Path.Combine(root, "cache"), Description: "repository cache");
        yield return new CleanupPath(Path.Combine(root, "artifacts"), Description: "artifacts");
    }
}

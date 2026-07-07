using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>uv (Astral) download and build cache.</summary>
public sealed class UvCleaner : DirectoryCleanerBase
{
    public override string Id => "uv";

    public override string Name => "uv cache";

    public override string Category => Categories.Python;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return new CleanupPath(
            OsPaths.Env(env, "UV_CACHE_DIR") ?? OsPaths.AppCache(env, Path.Combine("uv", "cache"), "uv", "uv"));
    }
}

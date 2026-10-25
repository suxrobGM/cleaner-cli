using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>uv (Astral) download and build cache. Prefers <c>uv cache clean</c>.</summary>
public sealed class UvCleaner : ProcessCleanerBase
{
    public override string Id => "uv";

    public override string Name => "uv cache";

    public override string Category => Categories.Python;

    protected override string Executable => "uv";

    protected override IReadOnlyList<string> CleanArguments => ["cache", "clean"];

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return new CleanupPath(
            OsPaths.Env(env, "UV_CACHE_DIR") ?? OsPaths.AppCache(env, Path.Combine("uv", "cache"), "uv", "uv"));
    }
}

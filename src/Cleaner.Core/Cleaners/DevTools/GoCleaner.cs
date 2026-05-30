using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>Go module and build caches. Prefers <c>go clean -cache -modcache</c>.</summary>
public sealed class GoCleaner : ProcessCleanerBase
{
    public override string Id => "go";

    public override string Name => "Go module & build cache";

    public override string Category => Categories.Go;

    protected override string Executable => "go";

    protected override IReadOnlyList<string> CleanArguments => ["clean", "-cache", "-modcache"];

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;

        var goPath = env.GetEnvironmentVariable("GOPATH");
        var modCache = !string.IsNullOrWhiteSpace(goPath)
            ? Path.Combine(goPath, "pkg", "mod")
            : env.HomePath("go", "pkg", "mod");
        yield return new CleanupPath(modCache, Description: "module cache");

        var goCache = env.GetEnvironmentVariable("GOCACHE");
        var buildCache = !string.IsNullOrWhiteSpace(goCache)
            ? goCache
            : OsPaths.AppCache(env, "go-build", "go-build", "go-build");
        yield return new CleanupPath(buildCache, Description: "build cache");
    }
}

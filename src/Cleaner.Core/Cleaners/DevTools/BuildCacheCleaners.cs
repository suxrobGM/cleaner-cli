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

/// <summary>Bazel disk cache and repository cache.</summary>
public sealed class BazelCleaner : DirectoryCleanerBase
{
    public override string Id => "bazel";

    public override string Name => "Bazel cache";

    public override string Category => Categories.BuildCaches;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        [new CleanupPath(Path.Combine(context.Environment.CacheDirectory, "bazel"), DeleteMode.ClearContents)];
}

/// <summary>Turborepo and Nx remote/local task caches (global and project-local).</summary>
public sealed class TurboNxCleaner : DirectoryCleanerBase
{
    public override string Id => "turbo-nx";

    public override string Name => "Turbo / Nx cache";

    public override string Category => Categories.BuildCaches;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return new CleanupPath(Path.Combine(env.CacheDirectory, "turbo"), Description: "global turbo cache");
        yield return new CleanupPath(Path.Combine(env.CacheDirectory, "nx"), Description: "global nx cache");
        yield return new CleanupPath(Path.Combine(context.WorkingDirectory, ".turbo"), Description: "project .turbo");
        yield return new CleanupPath(Path.Combine(context.WorkingDirectory, ".nx", "cache"), Description: "project .nx/cache");
    }
}

/// <summary>The <c>node_modules/.cache</c> directory under the working directory.</summary>
public sealed class NodeModulesCacheCleaner : DirectoryCleanerBase
{
    public override string Id => "node-modules-cache";

    public override string Name => "node_modules/.cache";

    public override string Category => Categories.BuildCaches;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        [new CleanupPath(Path.Combine(context.WorkingDirectory, "node_modules", ".cache"))];
}

using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>Helm chart repository cache (indexes and downloaded charts).</summary>
public sealed class HelmCleaner : DirectoryCleanerBase
{
    public override string Id => "helm";

    public override string Name => "Helm repository cache";

    public override string Category => Categories.Containers;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        if (OsPaths.Env(env, "HELM_REPOSITORY_CACHE") is { } configured)
        {
            yield return new CleanupPath(configured);
            yield break;
        }

        // Helm keeps its cache under the temp dir on Windows and the cache root elsewhere.
        yield return env.IsWindows
            ? new CleanupPath(Path.Combine(env.TempDirectory, "helm"))
            : new CleanupPath(Path.Combine(env.CacheDirectory, "helm"));
    }
}

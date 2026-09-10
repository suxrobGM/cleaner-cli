using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>Weights &amp; Biases artifact/download cache; run data under ~/wandb is never touched.</summary>
public sealed class WandbCleaner : DirectoryCleanerBase
{
    public override string Id => "wandb";

    public override string Name => "Weights & Biases cache";

    public override string Category => Categories.MachineLearning;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        if (OsPaths.Env(env, "WANDB_CACHE_DIR") is { } configured)
        {
            yield return new CleanupPath(configured, DeleteMode.ClearContents);
            yield break;
        }

        yield return new CleanupPath(env.HomePath(".cache", "wandb"), DeleteMode.ClearContents);
        if (env.IsMacOs)
        {
            yield return new CleanupPath(Path.Combine(env.HomeDirectory, "Library", "Caches", "wandb"), DeleteMode.ClearContents);
        }
    }
}

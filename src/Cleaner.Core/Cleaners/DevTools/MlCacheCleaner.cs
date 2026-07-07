using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// HuggingFace and Torch model caches under <c>~/.cache</c> (re-downloaded on demand). Honors the
/// <c>HF_HOME</c>/<c>HF_HUB_CACHE</c>/<c>TRANSFORMERS_CACHE</c>/<c>HF_DATASETS_CACHE</c>/<c>TORCH_HOME</c>
/// overrides. Leaves installed model registries like <c>~/.ollama/models</c> alone.
/// </summary>
public sealed class MlCacheCleaner : DirectoryCleanerBase
{
    public override string Id => "ml-cache";

    public override string Name => "ML model caches (HuggingFace/torch)";

    public override string Category => Categories.MachineLearning;

    /// <summary>Env vars that relocate a specific HuggingFace sub-cache; covered in addition to HF_HOME.</summary>
    private static readonly string[] HuggingFaceCacheVars = ["HF_HUB_CACHE", "TRANSFORMERS_CACHE", "HF_DATASETS_CACHE"];

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;

        // HuggingFace: HF_HOME relocates the whole cache tree (hub + datasets); default ~/.cache/huggingface.
        yield return new CleanupPath(
            OsPaths.Env(env, "HF_HOME") ?? env.HomePath(".cache", "huggingface"), DeleteMode.ClearContents, "HuggingFace");

        // Cover explicit per-cache relocations too; ExistingTargets de-dupes against the default above.
        foreach (var name in HuggingFaceCacheVars)
        {
            if (OsPaths.Env(env, name) is { } dir)
            {
                yield return new CleanupPath(dir, DeleteMode.ClearContents, "HuggingFace");
            }
        }

        // Torch hub pretrained weights.
        yield return new CleanupPath(
            OsPaths.Env(env, "TORCH_HOME") ?? env.HomePath(".cache", "torch"), DeleteMode.ClearContents, "Torch hub");

        // Keras downloaded datasets/weights and the kagglehub download cache.
        var kerasHome = OsPaths.Env(env, "KERAS_HOME") ?? env.HomePath(".keras");
        yield return new CleanupPath(Path.Combine(kerasHome, "datasets"), DeleteMode.ClearContents, "Keras datasets");
        yield return new CleanupPath(env.HomePath(".cache", "kagglehub"), DeleteMode.ClearContents, "kagglehub");
    }
}

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

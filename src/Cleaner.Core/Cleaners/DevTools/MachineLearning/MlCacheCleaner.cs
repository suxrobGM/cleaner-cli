using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// HuggingFace and Torch model caches (re-downloaded on demand), honoring their cache-location
/// environment variables. Installed model registries such as <c>~/.ollama/models</c> are preserved.
/// </summary>
/// <remarks>
/// Only the download caches under <c>HF_HOME</c> are cleared, never the root: it also holds the
/// login (<c>token</c>, <c>stored_tokens</c>).
/// </remarks>
public sealed class MlCacheCleaner : DirectoryCleanerBase
{
    public override string Id => "ml-cache";

    public override string Name => "ML model caches (HuggingFace/torch)";

    public override string Category => Categories.MachineLearning;

    /// <summary>Download caches under HF_HOME: models, Xet chunks, and datasets.</summary>
    private static readonly string[] HuggingFaceCaches = ["hub", "xet", "datasets"];

    /// <summary>Env vars that relocate a specific HuggingFace sub-cache; covered in addition to HF_HOME.</summary>
    private static readonly string[] HuggingFaceCacheVars =
        ["HF_HUB_CACHE", "TRANSFORMERS_CACHE", "HF_XET_CACHE", "HF_DATASETS_CACHE"];

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;

        // HuggingFace: HF_HOME relocates the whole tree; default ~/.cache/huggingface.
        var hfHome = OsPaths.Env(env, "HF_HOME") ?? env.HomePath(".cache", "huggingface");
        foreach (var cache in HuggingFaceCaches)
        {
            yield return new CleanupPath(Path.Combine(hfHome, cache), DeleteMode.ClearContents, "HuggingFace");
        }

        // Cover explicit per-cache relocations too; ExistingTargets de-dupes against the defaults above.
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

using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// HuggingFace, Torch, Keras, and kagglehub download caches. Local model stores such as
/// <c>~/.ollama/models</c> are kept.
/// </summary>
/// <remarks>
/// The <c>HF_HOME</c> root itself is never cleared: it also holds the login (<c>token</c>,
/// <c>stored_tokens</c>).
/// </remarks>
public sealed class MlCacheCleaner : DirectoryCleanerBase
{
    public override string Id => "ml-cache";

    public override string Name => "ML model caches (HuggingFace/torch)";

    public override string Category => Categories.MachineLearning;

    /// <summary>Models, Xet chunks, and datasets under HF_HOME.</summary>
    private static readonly string[] HuggingFaceCaches = ["hub", "xet", "datasets"];

    /// <summary>Env vars that move a single HuggingFace cache out of HF_HOME.</summary>
    private static readonly string[] HuggingFaceCacheVars =
        ["HF_HUB_CACHE", "TRANSFORMERS_CACHE", "HF_XET_CACHE", "HF_DATASETS_CACHE"];

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;

        var hfHome = OsPaths.Env(env, "HF_HOME") ?? env.HomePath(".cache", "huggingface");
        foreach (var cache in HuggingFaceCaches)
        {
            yield return new CleanupPath(Path.Combine(hfHome, cache), DeleteMode.ClearContents, "HuggingFace");
        }

        // ExistingTargets drops duplicates of the defaults above.
        foreach (var name in HuggingFaceCacheVars)
        {
            if (OsPaths.Env(env, name) is { } dir)
            {
                yield return new CleanupPath(dir, DeleteMode.ClearContents, "HuggingFace");
            }
        }

        yield return new CleanupPath(
            OsPaths.Env(env, "TORCH_HOME") ?? env.HomePath(".cache", "torch"), DeleteMode.ClearContents, "Torch hub");

        var kerasHome = OsPaths.Env(env, "KERAS_HOME") ?? env.HomePath(".keras");
        yield return new CleanupPath(Path.Combine(kerasHome, "datasets"), DeleteMode.ClearContents, "Keras datasets");
        yield return new CleanupPath(env.HomePath(".cache", "kagglehub"), DeleteMode.ClearContents, "kagglehub");
    }
}

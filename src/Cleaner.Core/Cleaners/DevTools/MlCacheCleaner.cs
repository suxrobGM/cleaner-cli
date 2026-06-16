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
        string? Configured(string name)
        {
            var value = env.GetEnvironmentVariable(name);
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        // HuggingFace: HF_HOME relocates the whole cache tree (hub + datasets); default ~/.cache/huggingface.
        yield return new CleanupPath(
            Configured("HF_HOME") ?? env.HomePath(".cache", "huggingface"), DeleteMode.ClearContents, "HuggingFace");

        // Cover explicit per-cache relocations too; ExistingTargets de-dupes against the default above.
        foreach (var name in HuggingFaceCacheVars)
        {
            if (Configured(name) is { } dir)
            {
                yield return new CleanupPath(dir, DeleteMode.ClearContents, "HuggingFace");
            }
        }

        // Torch hub pretrained weights.
        yield return new CleanupPath(
            Configured("TORCH_HOME") ?? env.HomePath(".cache", "torch"), DeleteMode.ClearContents, "Torch hub");
    }
}

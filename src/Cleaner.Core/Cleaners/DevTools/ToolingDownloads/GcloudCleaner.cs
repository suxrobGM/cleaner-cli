using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>Google Cloud CLI logs and surface caches; config and credentials are never touched.</summary>
public sealed class GcloudCleaner : DirectoryCleanerBase
{
    public override string Id => "gcloud";

    public override string Name => "gcloud logs & cache";

    public override string Category => Categories.ToolingDownloads;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        var config = OsPaths.Env(env, "CLOUDSDK_CONFIG")
            ?? (env.IsWindows
                ? Path.Combine(env.AppDataDirectory, "gcloud")
                : Path.Combine(env.HomeDirectory, ".config", "gcloud"));

        yield return new CleanupPath(Path.Combine(config, "logs"), DeleteMode.ClearContents, "logs");
        yield return new CleanupPath(Path.Combine(config, "surface_data"), DeleteMode.ClearContents, "surface cache");
    }
}

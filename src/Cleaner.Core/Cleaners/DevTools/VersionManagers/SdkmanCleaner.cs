using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>SDKMAN! downloaded archives and temp; installed candidates are kept.</summary>
public sealed class SdkmanCleaner : DirectoryCleanerBase
{
    public override string Id => "sdkman";

    public override string Name => "SDKMAN! archives";

    public override string Category => Categories.ToolingDownloads;

    public override bool IsApplicable(CleanupContext context) => !context.Environment.IsWindows;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        var root = OsPaths.Env(env, "SDKMAN_DIR") ?? env.HomePath(".sdkman");
        yield return new CleanupPath(Path.Combine(root, "archives"), DeleteMode.ClearContents, "archives");
        yield return new CleanupPath(Path.Combine(root, "tmp"), DeleteMode.ClearContents, "temp");
    }
}

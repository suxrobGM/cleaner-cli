using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>nvm's download cache (installed Node versions are kept).</summary>
public sealed class NvmCleaner : DirectoryCleanerBase
{
    public override string Id => "nvm";

    public override string Name => "nvm download cache";

    public override string Category => Categories.ToolingDownloads;

    // nvm-windows keeps installed runtimes (not a cache) under its root, so this is POSIX-only.
    public override bool IsApplicable(CleanupContext context) => !context.Environment.IsWindows;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        var root = OsPaths.Env(env, "NVM_DIR") ?? env.HomePath(".nvm");
        yield return new CleanupPath(Path.Combine(root, ".cache"), DeleteMode.ClearContents);
    }
}

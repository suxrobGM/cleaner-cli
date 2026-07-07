using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>Corepack's downloaded package-manager binaries (yarn/pnpm shims re-fetch on demand).</summary>
public sealed class CorepackCleaner : DirectoryCleanerBase
{
    public override string Id => "corepack";

    public override string Name => "Corepack cache";

    public override string Category => Categories.ToolingDownloads;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        if (OsPaths.Env(env, "COREPACK_HOME") is { } configured)
        {
            yield return new CleanupPath(configured, DeleteMode.ClearContents);
            yield break;
        }

        yield return new CleanupPath(env.HomePath(".cache", "node", "corepack"), DeleteMode.ClearContents);
        if (env.IsWindows)
        {
            yield return new CleanupPath(
                Path.Combine(env.LocalAppDataDirectory, "node", "corepack"), DeleteMode.ClearContents);
        }
    }
}

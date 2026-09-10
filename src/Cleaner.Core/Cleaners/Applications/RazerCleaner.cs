using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.Applications;

/// <summary>
/// Razer Cortex caches and its FPS-counter history. <c>CortexFPSData.db3</c> records every measured
/// session and is never pruned, so it grows without bound; Cortex recreates it empty. Game library
/// config, macros, and Synapse device profiles are untouched.
/// </summary>
public sealed class RazerCleaner : DirectoryCleanerBase
{
    public override string Id => "razer";

    public override string Name => "Razer Cortex caches";

    public override string Category => Categories.DesktopApps;

    public override bool IsApplicable(CleanupContext context) => context.Environment.IsWindows;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var cortex = OsPaths.ProgramData(context.Environment, "Razer", "RazerCortex");
        if (cortex is null)
        {
            yield break;
        }

        yield return new CleanupPath(Path.Combine(cortex, "CortexFPSData.db3"), DeleteMode.DeleteFile, "FPS history");
        yield return new CleanupPath(Path.Combine(cortex, "Log"), DeleteMode.ClearContents, "logs");
        yield return new CleanupPath(Path.Combine(cortex, "NativeTmp"), DeleteMode.ClearContents, "temp");
        yield return new CleanupPath(Path.Combine(cortex, "Update"), DeleteMode.ClearContents, "downloaded updates");
        yield return new CleanupPath(Path.Combine(cortex, "GameCovers"), DeleteMode.ClearContents, "game artwork");
        yield return new CleanupPath(Path.Combine(cortex, "GameLibCache"), DeleteMode.ClearContents, "game library cache");
    }
}

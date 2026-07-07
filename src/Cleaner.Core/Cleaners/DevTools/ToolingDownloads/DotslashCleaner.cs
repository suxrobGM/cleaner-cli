using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>DotSlash fetched-executable cache.</summary>
public sealed class DotslashCleaner : DirectoryCleanerBase
{
    public override string Id => "dotslash";

    public override string Name => "DotSlash cache";

    public override string Category => Categories.ToolingDownloads;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        [new CleanupPath(OsPaths.AppCache(context.Environment, "dotslash", "dotslash", "dotslash"), DeleteMode.ClearContents)];
}

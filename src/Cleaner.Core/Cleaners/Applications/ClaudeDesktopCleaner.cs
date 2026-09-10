using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.Applications;

/// <summary>
/// Claude Desktop's re-downloadable local-agent VM images. Chat history, settings, Chromium caches,
/// and profiles left by uninstalled copies are handled by separate cleaners.
/// </summary>
public sealed class ClaudeDesktopCleaner : DirectoryCleanerBase
{
    public override string Id => "claude-desktop";

    public override string Name => "Claude Desktop VM images";

    public override string Category => Categories.DesktopApps;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var root = OsPaths.AppData(context.Environment, "Claude", "Claude", "Claude");

        yield return new CleanupPath(Path.Combine(root, "vm_bundles"), Description: "agent VM bundle");
        yield return new CleanupPath(Path.Combine(root, "claude-code-vm"), Description: "agent VM state");
    }
}

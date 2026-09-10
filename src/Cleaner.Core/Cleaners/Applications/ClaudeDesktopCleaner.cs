using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.Applications;

/// <summary>
/// Claude Desktop's local-agent virtual machine images: a multi-GB rootfs downloaded on demand and
/// re-fetched the next time a local agent runs. Chat history and settings are left alone. The app's
/// Chromium caches are covered by <c>electron-app-cache</c>, and a profile left behind by an
/// uninstalled copy by <c>app-leftovers</c>.
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

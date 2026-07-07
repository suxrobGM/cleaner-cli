using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>Electron and electron-builder download caches.</summary>
public sealed class ElectronCacheCleaner : DirectoryCleanerBase
{
    public override string Id => "electron";

    public override string Name => "Electron caches";

    public override string Category => Categories.ToolingDownloads;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return new CleanupPath(OsPaths.AppCache(env, Path.Combine("electron", "Cache"), "electron", "electron"), Description: "Electron");
        yield return new CleanupPath(OsPaths.AppCache(env, Path.Combine("electron-builder", "Cache"), "electron-builder", "electron-builder"), Description: "electron-builder");
    }
}

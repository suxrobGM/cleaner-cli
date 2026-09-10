using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Services;

namespace Cleaner.Core.Cleaners.Os;

/// <summary>
/// Per-package caches and scratch state of Microsoft Store/UWP apps. Only known cache subdirectories
/// are cleared; <c>LocalState</c> and <c>Settings</c> are preserved.
/// </summary>
public sealed class StoreAppCacheCleaner : WindowsCleanerBase
{
    private static readonly string[] CacheSubdirectories =
    [
        Path.Combine("AC", "INetCache"),
        Path.Combine("AC", "Temp"),
        "TempState",
    ];

    public override string Id => "store-app-cache";

    public override string Name => "Store app caches";

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var packages = Path.Combine(context.Environment.LocalAppDataDirectory, "Packages");
        foreach (var package in context.FileSystem.EnumerateDirectories(packages))
        {
            foreach (var sub in CacheSubdirectories)
            {
                yield return new CleanupPath(Path.Combine(package, sub), DeleteMode.ClearContents);
            }
        }
    }
}

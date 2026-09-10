using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.Os;

/// <summary>User caches and logs under ~/Library (macOS).</summary>
public sealed class MacUserCachesCleaner : DirectoryCleanerBase
{
    public override string Id => "mac-caches";

    public override string Name => "macOS user caches & logs";

    public override string Category => Categories.OperatingSystem;

    public override bool IsApplicable(CleanupContext context) => context.Environment.IsMacOs;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var home = context.Environment.HomeDirectory;
        yield return new CleanupPath(Path.Combine(home, "Library", "Caches"), DeleteMode.ClearContents, "caches");
        yield return new CleanupPath(Path.Combine(home, "Library", "Logs"), DeleteMode.ClearContents, "logs");
    }
}

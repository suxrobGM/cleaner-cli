using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>CocoaPods spec-repo and pod download cache (macOS).</summary>
public sealed class CocoaPodsCleaner : DirectoryCleanerBase
{
    public override string Id => "cocoapods";

    public override string Name => "CocoaPods cache";

    public override string Category => Categories.Mobile;

    public override bool IsApplicable(CleanupContext context) => context.Environment.IsMacOs;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        [new CleanupPath(Path.Combine(context.Environment.HomeDirectory, "Library", "Caches", "CocoaPods"), DeleteMode.ClearContents)];
}

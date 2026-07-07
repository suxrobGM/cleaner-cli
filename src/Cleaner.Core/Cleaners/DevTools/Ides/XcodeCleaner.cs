using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// Xcode derived data, device-support symbol caches, and simulator caches (macOS). All are
/// regenerated on the next build or device connect; Archives (user data) are never touched.
/// </summary>
public sealed class XcodeCleaner : DirectoryCleanerBase
{
    public override string Id => "xcode";

    public override string Name => "Xcode caches";

    public override string Category => Categories.Ides;

    public override bool IsApplicable(CleanupContext context) => context.Environment.IsMacOs;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var developer = Path.Combine(context.Environment.HomeDirectory, "Library", "Developer");
        yield return new CleanupPath(
            Path.Combine(developer, "Xcode", "DerivedData"), DeleteMode.ClearContents, "DerivedData");
        yield return new CleanupPath(
            Path.Combine(developer, "Xcode", "iOS DeviceSupport"), DeleteMode.ClearContents, "iOS device support");
        yield return new CleanupPath(
            Path.Combine(developer, "Xcode", "watchOS DeviceSupport"), DeleteMode.ClearContents, "watchOS device support");
        yield return new CleanupPath(
            Path.Combine(developer, "CoreSimulator", "Caches"), DeleteMode.ClearContents, "simulator caches");
    }
}

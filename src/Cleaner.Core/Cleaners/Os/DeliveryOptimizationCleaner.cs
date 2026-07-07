using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Services;

namespace Cleaner.Core.Cleaners.Os;

/// <summary>Windows Delivery Optimization download cache. Needs elevation.</summary>
public sealed class DeliveryOptimizationCleaner : WindowsCleanerBase
{
    public override string Id => "delivery-optimization";

    public override string Name => "Delivery Optimization cache";

    public override bool RequiresElevation => true;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var windows = context.Environment.WindowsDirectory;
        if (windows is null)
        {
            yield break;
        }

        var root = Path.GetPathRoot(windows) ?? "C:\\";
        yield return new CleanupPath(
            Path.Combine(root, "Windows", "ServiceProfiles", "NetworkService", "AppData", "Local",
                "Microsoft", "Windows", "DeliveryOptimization", "Cache"),
            DeleteMode.ClearContents);
    }
}

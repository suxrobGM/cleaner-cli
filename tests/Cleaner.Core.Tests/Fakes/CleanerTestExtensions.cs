using Cleaner.Core.Abstractions;

namespace Cleaner.Core.Tests.Fakes;

public static class CleanerTestExtensions
{
    /// <summary>Scans, then cleans everything the scan reported, like a fully approved plan.</summary>
    public static async Task<CleanResult> CleanAsync(this ICleaner cleaner, CleanupContext context)
    {
        var scan = await cleaner.ScanAsync(context);
        return await cleaner.CleanAsync(context, scan.Targets);
    }
}

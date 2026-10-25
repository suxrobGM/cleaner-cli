using Cleaner.Core.Abstractions;

namespace Cleaner.Core.Tests.Fakes;

public static class CleanerTestExtensions
{
    /// <summary>
    /// Scan, then clean everything the scan reported, the way the app runs a fully approved plan.
    /// Keeps tests that only care about the end state short.
    /// </summary>
    public static async Task<CleanResult> CleanAsync(this ICleaner cleaner, CleanupContext context)
    {
        var scan = await cleaner.ScanAsync(context);
        return await cleaner.CleanAsync(context, scan.Targets);
    }
}

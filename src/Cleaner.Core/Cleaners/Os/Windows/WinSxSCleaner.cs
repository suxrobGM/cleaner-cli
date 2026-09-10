using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Services;
using Cleaner.Core.Utils;

namespace Cleaner.Core.Cleaners.Os;

/// <summary>
/// Cleans superseded Windows component versions with <c>DISM /StartComponentCleanup</c>.
/// Omits <c>/ResetBase</c> so updates remain uninstallable.
/// </summary>
/// <remarks>
/// WinSxS is mostly hard links, so DISM's <c>/AnalyzeComponentStore</c> is required to identify
/// reclaimable bytes. Scans return no estimate when DISM is unavailable, unelevated, or localized.
/// </remarks>
public sealed class WinSxSCleaner : ProcessCleanerBase
{
    private static readonly string[] AnalyzeArguments = ["/Online", "/Cleanup-Image", "/AnalyzeComponentStore"];

    /// <summary>Labels DISM prints for the parts of the store a cleanup actually removes.</summary>
    private static readonly string[] ReclaimableLabels = ["Backups and Disabled Features", "Cache and Temporary Data"];

    private const string ActualSizeLabel = "Actual Size of Component Store";

    public override string Id => "winsxs";

    public override string Name => "Windows component store (WinSxS)";

    public override string Category => Categories.SystemCaches;

    public override bool RequiresElevation => true;

    public override bool SupportsSizeEstimate => false;

    public override bool IsApplicable(CleanupContext context) => context.Environment.IsWindows;

    protected override string Executable => "dism";

    protected override IReadOnlyList<string> CleanArguments =>
        ["/Online", "/Cleanup-Image", "/StartComponentCleanup"];

    public override async Task<ScanResult> ScanAsync(CleanupContext context, CancellationToken cancellationToken = default)
    {
        var report = await AnalyzeAsync(context, cancellationToken).ConfigureAwait(false);
        var reclaimable = report is null ? 0 : ReclaimableLabels.Sum(label => Measure(report, label));

        // Reuse the report because each DISM analysis is expensive.
        RememberMeasurement(context, StoreSize(report));
        return reclaimable > 0
            ? new ScanResult([new CleanupTarget("WinSxS", reclaimable, "superseded components, backups, and servicing scratch")])
            : ScanResult.Empty;
    }

    /// <summary>
    /// Measures the store through DISM so results reflect the actual store size.
    /// </summary>
    protected override async ValueTask<long?> MeasureAsync(CleanupContext context, CancellationToken cancellationToken) =>
        StoreSize(await AnalyzeAsync(context, cancellationToken).ConfigureAwait(false));

    /// <summary>The store size a report gives, or null when there is no usable number in it.</summary>
    private static long? StoreSize(string? report)
    {
        var size = Measure(report, ActualSizeLabel);
        return size > 0 ? size : null;
    }

    /// <summary>DISM's component store report, or null when it can't be produced.</summary>
    private static async Task<string?> AnalyzeAsync(CleanupContext context, CancellationToken cancellationToken)
    {
        // Analysis needs the same elevation the cleanup does; asking without it just fails slowly.
        if (!context.Environment.IsElevated || !context.ProcessRunner.Exists("dism"))
        {
            return null;
        }

        var result = await context.ProcessRunner
            .RunAsync("dism", AnalyzeArguments, cancellationToken)
            .ConfigureAwait(false);

        return result.Success ? result.StandardOutput : null;
    }

    /// <summary>Bytes on the <c>Label : 1.23 GB</c> line of a DISM report, or 0 if it isn't there.</summary>
    private static long Measure(string? report, string label)
    {
        if (report is null)
        {
            return 0;
        }

        foreach (var line in report.Split('\n'))
        {
            var marker = line.IndexOf(label, StringComparison.OrdinalIgnoreCase);
            if (marker < 0)
            {
                continue;
            }

            var separator = line.IndexOf(':', marker + label.Length);
            if (separator >= 0 && SizeParser.TryParse(line[(separator + 1)..], out var bytes))
            {
                return bytes;
            }
        }

        return 0;
    }
}

using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Services;
using Cleaner.Core.Utils;

namespace Cleaner.Core.Cleaners.Os;

/// <summary>
/// Windows component store (WinSxS) cleanup via <c>DISM /StartComponentCleanup</c> — removes
/// superseded component versions. Deliberately no <c>/ResetBase</c>, which would prevent
/// uninstalling updates. Slow (minutes) but the largest legitimate Windows reclaim.
/// </summary>
/// <remarks>
/// WinSxS is mostly hard links into the live system, so measuring the folder tells you nothing about
/// what is removable. DISM's own <c>/AnalyzeComponentStore</c> is the only source of that number, so
/// the scan runs it — that costs the best part of a minute, and reports nothing on a machine where
/// DISM is missing, the shell is not elevated, or the output is not in English.
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

        // This report also carries the store size the clean measures against, and producing another
        // one costs the best part of a minute — so the clean spends this one instead.
        RememberMeasurement(context, StoreSize(report));
        return reclaimable > 0
            ? new ScanResult([new CleanupTarget("WinSxS", reclaimable, "superseded components, backups, and servicing scratch")])
            : ScanResult.Empty;
    }

    /// <summary>
    /// Size the store from DISM itself, so the summary reports what the store actually gave back
    /// rather than the estimate. Each measurement adds about a minute to an already slow operation.
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

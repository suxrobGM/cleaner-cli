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
/// WinSxS is mostly hard links, so only DISM can size it — and its report costs a full walk of the
/// component store, which takes minutes on an ordinary machine and blocks outright while Windows
/// servicing holds its lock. Scanning never pays that: this cleaner reports its size after running,
/// like the other command-driven ones. Both DISM calls carry a deadline so neither can stall a run.
/// </remarks>
public sealed class WinSxSCleaner : ProcessCleanerBase
{
    private static readonly string[] AnalyzeArguments = ["/Online", "/Cleanup-Image", "/AnalyzeComponentStore"];

    /// <summary>Ceiling for a store report, which walks every component installed on the machine.</summary>
    private static readonly TimeSpan AnalyzeTimeout = TimeSpan.FromMinutes(10);

    /// <summary>Ceiling for the cleanup, which rebuilds the store and is slower again.</summary>
    private static readonly TimeSpan CleanupTimeout = TimeSpan.FromMinutes(45);

    private const string ActualSizeLabel = "Actual Size of Component Store";

    public override string Id => "winsxs";

    public override string Name => "Windows component store (WinSxS)";

    public override string Category => Categories.SystemCaches;

    public override bool RequiresElevation => true;

    public override bool SupportsSizeEstimate => false;

    public override string ConfirmationWarning =>
        "DISM rebuilds the component store, so this one runs for many minutes with no progress of " +
        "its own — leave it out if you want the rest of the run to finish quickly";

    public override bool IsApplicable(CleanupContext context) => context.Environment.IsWindows;

    protected override string Executable => "dism";

    protected override TimeSpan? CommandTimeout => CleanupTimeout;

    protected override IReadOnlyList<string> CleanArguments =>
        ["/Online", "/Cleanup-Image", "/StartComponentCleanup"];

    /// <summary>
    /// Measures the store through DISM so results reflect the actual store size. Only the clean
    /// path calls this, because the report is far too slow to run during a scan.
    /// </summary>
    protected override async ValueTask<long?> MeasureAsync(CleanupContext context, CancellationToken cancellationToken)
    {
        // Analysis needs the same elevation the cleanup does; asking without it just fails slowly.
        if (!context.Environment.IsElevated || !context.ProcessRunner.Exists("dism"))
        {
            return null;
        }

        var result = await context.ProcessRunner
            .RunAsync("dism", AnalyzeArguments, AnalyzeTimeout, cancellationToken)
            .ConfigureAwait(false);

        var size = result.Success ? StoreSize(result.StandardOutput) : 0;
        return size > 0 ? size : null;
    }

    /// <summary>Bytes on the report's <c>Actual Size of Component Store : 1.23 GB</c> line, else 0.</summary>
    private static long StoreSize(string report)
    {
        foreach (var line in report.Split('\n'))
        {
            var marker = line.IndexOf(ActualSizeLabel, StringComparison.OrdinalIgnoreCase);
            if (marker < 0)
            {
                continue;
            }

            var separator = line.IndexOf(':', marker + ActualSizeLabel.Length);
            if (separator >= 0 && SizeParser.TryParse(line[(separator + 1)..], out var bytes))
            {
                return bytes;
            }
        }

        return 0;
    }
}

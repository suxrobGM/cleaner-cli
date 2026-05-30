using Cleaner.Core.Abstractions;
using Cleaner.Core.Services;

namespace Cleaner.Core.Cleaners;

/// <summary>
/// Runs a single cleaner's scan/clean defensively: an unexpected failure becomes a reported result
/// plus a log entry instead of propagating, so one misbehaving cleaner never aborts a whole run.
/// Cancellation still propagates so the user can abort the run as a whole.
/// </summary>
public static class CleanerRunner
{
    public static async Task<ScanResult> SafeScanAsync(
        ICleaner cleaner,
        CleanupContext context,
        IAppLogger logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await cleaner.ScanAsync(context, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.Error($"Cleaner '{cleaner.Id}' failed during scan", ex);
            return ScanResult.Empty;
        }
    }

    public static async Task<CleanResult> SafeCleanAsync(
        ICleaner cleaner,
        CleanupContext context,
        IAppLogger logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await cleaner.CleanAsync(context, progress: null, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.Error($"Cleaner '{cleaner.Id}' failed during clean", ex);
            return new CleanResult(0, 0, [$"Unexpected error: {ex.Message}"]);
        }
    }
}

using Cleaner.Core.Abstractions;
using Cleaner.Core.Services;

namespace Cleaner.Core.Cleaners;

/// <summary>
/// Logs a cleaner's unexpected failures instead of throwing, so one cleaner can't abort the run.
/// Cancellation still propagates.
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
        IReadOnlyList<CleanupTarget> approved,
        IAppLogger logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await cleaner.CleanAsync(context, approved, progress: null, cancellationToken).ConfigureAwait(false);
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

namespace Cleaner.Core.Services;

/// <summary>Helpers for external commands that must not hold a scan open indefinitely.</summary>
public static class ProcessRunnerExtensions
{
    /// <summary>
    /// Runs a command with a deadline. A deadline expiry is returned as a failed process result;
    /// cancellation requested by the caller still propagates.
    /// </summary>
    public static async Task<ProcessResult> RunWithTimeoutAsync(
        this IProcessRunner runner,
        string executable,
        IReadOnlyList<string> arguments,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(timeout, TimeSpan.Zero);

        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeout);

        try
        {
            return await runner.RunAsync(executable, arguments, deadline.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new ProcessResult(-1, string.Empty, $"{executable} timed out after {timeout.TotalSeconds:0} seconds");
        }
    }
}

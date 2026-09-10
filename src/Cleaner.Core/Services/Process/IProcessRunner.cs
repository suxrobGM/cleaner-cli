namespace Cleaner.Core.Services;

public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError)
{
    public bool Success => ExitCode == 0;

    /// <summary>Why the command failed: its stderr, or the exit code when it printed nothing.</summary>
    public string FailureMessage(string executable) =>
        string.IsNullOrWhiteSpace(StandardError)
            ? $"{executable} exited with code {ExitCode}"
            : StandardError.Trim();
}

/// <summary>Runs external tools through a testable process abstraction.</summary>
public interface IProcessRunner
{
    /// <summary>True if <paramref name="executable"/> can be found on PATH.</summary>
    bool Exists(string executable);

    /// <summary>
    /// Runs <paramref name="executable"/> to completion. A <paramref name="timeout"/> stops a
    /// command that would otherwise hold a scan open, and is reported as a failed result;
    /// cancellation requested by the caller propagates instead.
    /// </summary>
    Task<ProcessResult> RunAsync(
        string executable,
        IReadOnlyList<string> arguments,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default);
}

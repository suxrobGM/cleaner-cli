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

    /// <summary>Executable paths of running processes; ones this user can't inspect are left out.</summary>
    IReadOnlyList<string> RunningExecutablePaths();

    /// <summary>
    /// Runs to completion. Hitting <paramref name="timeout"/> returns a failed result so a hung tool can't
    /// stall a scan; caller cancellation throws instead.
    /// </summary>
    Task<ProcessResult> RunAsync(
        string executable,
        IReadOnlyList<string> arguments,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default);
}

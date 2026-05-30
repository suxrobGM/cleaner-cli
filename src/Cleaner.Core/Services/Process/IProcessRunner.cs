namespace Cleaner.Core.Services;

public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError)
{
    public bool Success => ExitCode == 0;
}

/// <summary>
/// Runs external tools for cleaners that delegate to a native command (e.g. <c>docker system
/// prune</c>). Abstracted so process-based cleaners can be tested without spawning processes.
/// </summary>
public interface IProcessRunner
{
    /// <summary>True if <paramref name="executable"/> can be found on PATH.</summary>
    bool Exists(string executable);

    Task<ProcessResult> RunAsync(
        string executable,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken = default);
}

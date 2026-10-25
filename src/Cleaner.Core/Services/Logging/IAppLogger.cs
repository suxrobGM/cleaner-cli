namespace Cleaner.Core.Services;

/// <summary>Minimal logger abstraction that keeps Core independent of a logging framework.</summary>
public interface IAppLogger
{
    void Info(string message);

    void Warn(string message);

    void Error(string message, Exception? exception = null);

    /// <summary>
    /// A per-path line (removed, skipped, protected). Goes to the current run's own log file so the
    /// rolling log keeps its history; outside a run it is written to the main log.
    /// </summary>
    void Detail(string message);

    /// <summary>Start a run log, <c>runs/run-&lt;timestamp&gt;.log</c>, until the returned handle is disposed.</summary>
    IDisposable BeginRun();

    /// <summary>The active run's log file, or null outside a run.</summary>
    string? RunLogFilePath { get; }

    /// <summary>Absolute path of the active log file, shown to the user when something goes wrong.</summary>
    string LogFilePath { get; }
}

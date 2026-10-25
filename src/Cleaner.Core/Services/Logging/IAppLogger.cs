namespace Cleaner.Core.Services;

/// <summary>Minimal logger abstraction that keeps Core independent of a logging framework.</summary>
public interface IAppLogger
{
    void Info(string message);

    void Warn(string message);

    void Error(string message, Exception? exception = null);

    /// <summary>
    /// A per-path line (removed, skipped, protected). Written to the current run's log file so it doesn't
    /// push older history out of the rolling main log; outside a run it goes to the main log.
    /// </summary>
    void Detail(string message);

    /// <summary>Logs to <c>runs/run-&lt;timestamp&gt;.log</c> until the returned handle is disposed.</summary>
    IDisposable BeginRun();

    /// <summary>Null outside a run.</summary>
    string? RunLogFilePath { get; }

    /// <summary>Absolute path, shown to the user when something goes wrong.</summary>
    string LogFilePath { get; }
}

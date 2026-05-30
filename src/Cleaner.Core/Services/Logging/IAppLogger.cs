namespace Cleaner.Core.Services;

/// <summary>
/// Minimal application logger for crash diagnostics and per-cleaner error trails. Abstracted (like
/// <see cref="IProcessRunner"/>) so Core takes no logging-framework dependency; the CLI supplies the
/// concrete file-backed implementation.
/// </summary>
public interface IAppLogger
{
    void Info(string message);

    void Warn(string message);

    void Error(string message, Exception? exception = null);

    /// <summary>Absolute path of the active log file, shown to the user when something goes wrong.</summary>
    string LogFilePath { get; }
}

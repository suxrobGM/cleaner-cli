namespace Cleaner.Core.Services;

/// <summary>Minimal logger abstraction that keeps Core independent of a logging framework.</summary>
public interface IAppLogger
{
    void Info(string message);

    void Warn(string message);

    void Error(string message, Exception? exception = null);

    /// <summary>Absolute path of the active log file, shown to the user when something goes wrong.</summary>
    string LogFilePath { get; }
}

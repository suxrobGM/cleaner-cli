using Cleaner.Core.Services;

namespace Cleaner.Core.Tests.Fakes;

public sealed class NullAppLogger : IAppLogger
{
    public string LogFilePath => "(test)";

    public string? RunLogFilePath => null;

    public List<string> Lines { get; } = [];

    public void Info(string message) => Record(message);

    public void Warn(string message) => Record(message);

    public void Error(string message, Exception? exception = null) => Record(message);

    public void Detail(string message) => Record(message);

    public IDisposable BeginRun() => new Run();

    private void Record(string message)
    {
        lock (Lines)
        {
            Lines.Add(message);
        }
    }

    private sealed class Run : IDisposable
    {
        public void Dispose() { }
    }
}

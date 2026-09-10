using Cleaner.Core.Services;

namespace Cleaner.Core.Tests.Fakes;

public sealed class NullAppLogger : IAppLogger
{
    public string LogFilePath => "(test)";

    public void Info(string message) { }

    public void Warn(string message) { }

    public void Error(string message, Exception? exception = null) { }
}

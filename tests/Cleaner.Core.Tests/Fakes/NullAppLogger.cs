using Cleaner.Core.Services;

namespace Cleaner.Core.Tests.Fakes;

/// <summary>A no-op <see cref="IAppLogger"/> for tests that don't assert on log output.</summary>
public sealed class NullAppLogger : IAppLogger
{
    public string LogFilePath => "(test)";

    public void Info(string message) { }

    public void Warn(string message) { }

    public void Error(string message, Exception? exception = null) { }
}

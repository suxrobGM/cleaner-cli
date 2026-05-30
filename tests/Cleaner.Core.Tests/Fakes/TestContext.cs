using Cleaner.Core.Abstractions;

namespace Cleaner.Core.Tests.Fakes;

/// <summary>Convenience factory for a <see cref="CleanupContext"/> backed by fakes.</summary>
public static class TestContext
{
    public static CleanupContext Create(
        FakeFileSystem fileSystem,
        FakeEnvironment? environment = null,
        FakeProcessRunner? processRunner = null,
        bool dryRun = false,
        string? workingDirectory = null) => new()
        {
            FileSystem = fileSystem,
            Environment = environment ?? new FakeEnvironment(),
            ProcessRunner = processRunner ?? new FakeProcessRunner(),
            DryRun = dryRun,
            WorkingDirectory = workingDirectory ?? "/work",
        };
}

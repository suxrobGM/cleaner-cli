using Cleaner.Core.Abstractions;

namespace Cleaner.Core.Tests.Fakes;

public static class TestContext
{
    public static CleanupContext Create(
        FakeFileSystem? fileSystem = null,
        FakeEnvironment? environment = null,
        FakeProcessRunner? processRunner = null,
        bool dryRun = false,
        string? workingDirectory = null,
        IReadOnlyList<string>? scanRoots = null,
        IReadOnlySet<string>? selectedPaths = null) => new()
        {
            FileSystem = fileSystem ?? new FakeFileSystem(),
            Environment = environment ?? new FakeEnvironment(),
            ProcessRunner = processRunner ?? new FakeProcessRunner(),
            DryRun = dryRun,
            WorkingDirectory = workingDirectory ?? "/work",
            ScanRoots = scanRoots ?? [],
            SelectedPaths = selectedPaths,
        };
}

using Cleaner.Core.Abstractions;
using Cleaner.Core.Services;

namespace Cleaner.Core.Tests.Fakes;

public static class TestContext
{
    /// <summary>A context over the fakes, guarded by a real <see cref="DeletionGuard"/> built from them.</summary>
    public static CleanupContext Create(
        FakeFileSystem? fileSystem = null,
        FakeEnvironment? environment = null,
        FakeProcessRunner? processRunner = null,
        bool dryRun = false,
        string? workingDirectory = null,
        IReadOnlyList<string>? scanRoots = null,
        IAppLogger? logger = null)
    {
        var fs = fileSystem ?? new FakeFileSystem();
        var env = environment ?? new FakeEnvironment();
        var runner = processRunner ?? new FakeProcessRunner();
        return new()
        {
            FileSystem = fs,
            Environment = env,
            ProcessRunner = runner,
            Guard = DeletionGuard.Create(env, fs, runner),
            Logger = logger,
            DryRun = dryRun,
            WorkingDirectory = workingDirectory ?? "/work",
            ScanRoots = scanRoots ?? [],
        };
    }
}

using Cleaner.Core.Abstractions;
using Cleaner.Core.Services;

namespace Cleaner.Cli.Application;

public sealed class CleanupContextFactory(
    IFileSystemService fileSystem,
    IEnvironmentService environment,
    IProcessRunner processRunner,
    IAppLogger logger)
{
    /// <summary>Builds a new guard each time, so PATH and the exclude file are read fresh.</summary>
    public CleanupContext Create(RunOptions options) => new()
    {
        FileSystem = fileSystem,
        Environment = environment,
        ProcessRunner = processRunner,
        Guard = DeletionGuard.Create(environment, fileSystem, processRunner),
        Logger = logger,
        WorkingDirectory = options.WorkingDirectory,
        ScanRoots = options.ScanRoots,
    };
}

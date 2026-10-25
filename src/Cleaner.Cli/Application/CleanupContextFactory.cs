using Cleaner.Core.Abstractions;
using Cleaner.Core.Services;

namespace Cleaner.Cli.Application;

/// <summary>Builds the <see cref="CleanupContext"/> every cleaner runs against from a <see cref="RunOptions"/>.</summary>
public sealed class CleanupContextFactory(
    IFileSystemService fileSystem,
    IEnvironmentService environment,
    IProcessRunner processRunner,
    IAppLogger logger)
{
    /// <summary>A fresh context with its own guard, so PATH and the exclude file are read as they are now.</summary>
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

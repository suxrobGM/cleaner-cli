using Cleaner.Core.Abstractions;
using Cleaner.Core.Services;

namespace Cleaner.Cli.Application;

/// <summary>Builds the <see cref="CleanupContext"/> every cleaner runs against from a <see cref="RunOptions"/>.</summary>
public sealed class CleanupContextFactory(
    IFileSystemService fileSystem,
    IEnvironmentService environment,
    IProcessRunner processRunner)
{
    public CleanupContext Create(RunOptions options) => new()
    {
        FileSystem = fileSystem,
        Environment = environment,
        ProcessRunner = processRunner,
        DryRun = options.DryRun,
        Force = options.Force,
        WorkingDirectory = options.WorkingDirectory,
        ScanRoots = options.ScanRoots,
    };
}

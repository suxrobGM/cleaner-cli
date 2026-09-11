using Cleaner.Core.Abstractions;
using Cleaner.Core.Services;

namespace Cleaner.Cli.Application;

/// <summary>Builds the <see cref="CleanupContext"/> every cleaner runs against from a <see cref="RunOptions"/>.</summary>
public sealed class CleanupContextFactory(
    IFileSystemService fileSystem,
    IEnvironmentService environment,
    IProcessRunner processRunner)
{
    /// <param name="selectedPaths">Folders the user kept; null takes all.</param>
    public CleanupContext Create(RunOptions options, IReadOnlySet<string>? selectedPaths = null) => new()
    {
        FileSystem = fileSystem,
        Environment = environment,
        ProcessRunner = processRunner,
        WorkingDirectory = options.WorkingDirectory,
        ScanRoots = options.ScanRoots,
        SelectedPaths = selectedPaths,
    };
}

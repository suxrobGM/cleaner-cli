using Cleaner.Core.Services;

namespace Cleaner.Core.Abstractions;

/// <summary>Services and options shared by cleaners for cross-platform, testable I/O.</summary>
public sealed class CleanupContext
{
    public required IFileSystemService FileSystem { get; init; }

    public required IEnvironmentService Environment { get; init; }

    public required IProcessRunner ProcessRunner { get; init; }

    /// <summary>When true, cleaners measure and report but never delete.</summary>
    public bool DryRun { get; init; }

    /// <summary>Base directory for project-local sweeps (bin/obj, node_modules, ...). Defaults to cwd.</summary>
    public string WorkingDirectory { get; init; } = System.Environment.CurrentDirectory;

    /// <summary>Roots for workspace sweeps; defaults to <see cref="WorkingDirectory"/>.</summary>
    public IReadOnlyList<string> ScanRoots
    {
        get => _scanRoots is { Count: > 0 } ? _scanRoots : [WorkingDirectory];
        init => _scanRoots = value;
    }

    private readonly IReadOnlyList<string>? _scanRoots;
}

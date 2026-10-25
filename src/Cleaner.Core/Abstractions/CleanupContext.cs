using Cleaner.Core.Services;

namespace Cleaner.Core.Abstractions;

/// <summary>Services and options shared by cleaners for cross-platform, testable I/O.</summary>
public sealed record CleanupContext
{
    public required IFileSystemService FileSystem { get; init; }

    public required IEnvironmentService Environment { get; init; }

    public required IProcessRunner ProcessRunner { get; init; }

    /// <summary>Decides what may be deleted. Consulted at scan time and again before every removal.</summary>
    public required IDeletionGuard Guard { get; init; }

    /// <summary>Receives the per-path record of a clean: every removal, skip, and failure.</summary>
    public IAppLogger? Logger { get; init; }

    /// <summary>Identifies one scan-then-clean run, so a measurement taken by the scan is only reused by its clean.</summary>
    public Guid RunId { get; init; } = Guid.NewGuid();

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

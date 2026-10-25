using Cleaner.Core.Services;

namespace Cleaner.Core.Abstractions;

/// <summary>Services and options shared by cleaners for cross-platform, testable I/O.</summary>
public sealed record CleanupContext
{
    public required IFileSystemService FileSystem { get; init; }

    public required IEnvironmentService Environment { get; init; }

    public required IProcessRunner ProcessRunner { get; init; }

    /// <summary>Checked at scan time and again before every removal.</summary>
    public required IDeletionGuard Guard { get; init; }

    /// <summary>Receives every removal, skip, and failure of a clean.</summary>
    public IAppLogger? Logger { get; init; }

    /// <summary>Ties a scan to its clean, so a size measured by one scan is reused only by that clean.</summary>
    public Guid RunId { get; init; } = Guid.NewGuid();

    /// <summary>When true, cleaners measure and report but never delete.</summary>
    public bool DryRun { get; init; }

    /// <summary>Base directory for project-local cleaners (bin/obj, node_modules, ...).</summary>
    public string WorkingDirectory { get; init; } = System.Environment.CurrentDirectory;

    /// <summary>Folders searched for projects; defaults to <see cref="WorkingDirectory"/>.</summary>
    public IReadOnlyList<string> ScanRoots
    {
        get => _scanRoots is { Count: > 0 } ? _scanRoots : [WorkingDirectory];
        init => _scanRoots = value;
    }

    private readonly IReadOnlyList<string>? _scanRoots;
}

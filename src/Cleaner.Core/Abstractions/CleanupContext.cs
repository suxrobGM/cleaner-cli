using Cleaner.Core.Services;

namespace Cleaner.Core.Abstractions;

/// <summary>
/// Ambient services and options handed to every cleaner. Cleaners resolve all paths and run all
/// I/O through these abstractions so they stay cross-platform and unit-testable.
/// </summary>
public sealed class CleanupContext
{
    public required IFileSystemService FileSystem { get; init; }

    public required IEnvironmentService Environment { get; init; }

    public required IProcessRunner ProcessRunner { get; init; }

    /// <summary>When true, cleaners measure and report but never delete.</summary>
    public bool DryRun { get; init; }

    /// <summary>When true, cleaners may remove targets that are otherwise treated cautiously.</summary>
    public bool Force { get; init; }

    /// <summary>Base directory for project-local sweeps (bin/obj, node_modules, ...). Defaults to cwd.</summary>
    public string WorkingDirectory { get; init; } = System.Environment.CurrentDirectory;

    /// <summary>
    /// Roots that workspace-sweeping cleaners (build artifacts, Unity projects) recurse into. Set via
    /// <c>--path</c>; when none are given it falls back to <see cref="WorkingDirectory"/>.
    /// </summary>
    public IReadOnlyList<string> ScanRoots
    {
        get => _scanRoots is { Count: > 0 } ? _scanRoots : [WorkingDirectory];
        init => _scanRoots = value;
    }

    private readonly IReadOnlyList<string>? _scanRoots;
}

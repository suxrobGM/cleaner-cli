namespace Cleaner.Cli.Application;

/// <summary>Per-invocation options shared by the scan/clean/interactive flows.</summary>
public sealed record RunOptions
{
    public bool DryRun { get; init; }

    public bool Force { get; init; }

    /// <summary>Skip the confirmation prompt before deleting.</summary>
    public bool AssumeYes { get; init; }

    public string WorkingDirectory { get; init; } = Environment.CurrentDirectory;

    /// <summary>Roots (<c>--path</c>) for workspace-sweeping cleaners; empty falls back to cwd.</summary>
    public IReadOnlyList<string> ScanRoots { get; init; } = [];
}

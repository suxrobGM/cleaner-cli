namespace Cleaner.Cli.Application;

/// <summary>Per-invocation options shared by the scan/clean/interactive flows.</summary>
public sealed record RunOptions
{
    public bool DryRun { get; init; }

    public bool Force { get; init; }

    /// <summary>Skip the confirmation prompt before deleting.</summary>
    public bool AssumeYes { get; init; }

    public string WorkingDirectory { get; init; } = Environment.CurrentDirectory;
}

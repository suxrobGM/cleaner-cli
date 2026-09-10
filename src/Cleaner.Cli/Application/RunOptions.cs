namespace Cleaner.Cli.Application;

/// <summary>Per-invocation options shared by the interactive flows.</summary>
public sealed record RunOptions
{
    /// <summary>When true, measure and report but never delete. Set by the menu's preview action.</summary>
    public bool DryRun { get; init; }

    /// <summary>Show the per-target path breakdown in size tables.</summary>
    public bool Verbose { get; init; }

    public string WorkingDirectory { get; init; } = Environment.CurrentDirectory;

    /// <summary>Roots (<c>--path</c>) for workspace-sweeping cleaners; empty falls back to cwd.</summary>
    public IReadOnlyList<string> ScanRoots { get; init; } = [];
}

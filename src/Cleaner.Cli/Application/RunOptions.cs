namespace Cleaner.Cli.Application;

public sealed record RunOptions
{
    /// <summary>Show the per-target path breakdown in size tables.</summary>
    public bool Verbose { get; init; }

    public string WorkingDirectory { get; init; } = Environment.CurrentDirectory;

    /// <summary>Folders from <c>--path</c> to search for projects; empty means the working directory.</summary>
    public IReadOnlyList<string> ScanRoots { get; init; } = [];
}

using System.CommandLine;
using Cleaner.Cli.Application;

namespace Cleaner.Cli.Commands;

/// <summary>Builds the command-line root and hands control to the interactive menu.</summary>
public sealed class CommandLineBuilder(CleanerApp app)
{
    private readonly Option<string[]> pathOption = new("--path", "-p")
    {
        Description = "Directory for project-local cleaners; repeat to sweep several workspaces (defaults to cwd).",
        AllowMultipleArgumentsPerToken = true,
    };

    private readonly Option<bool> verboseOption = new("--verbose", "-v")
    {
        Description = "Show the individual directories behind each cleaner's size.",
    };

    private readonly Option<bool> checkOption = new("--check")
    {
        Description = "Only check for a newer release; don't download or install.",
    };

    public RootCommand Build()
    {
        var updateCommand = new Command("update", "Check for a newer release and install it.")
        {
            checkOption,
        };
        updateCommand.SetAction((result, ct) => app.UpdateAsync(result.GetValue(checkOption), ct));

        var root = new RootCommand("Cleaner — reclaim disk space by clearing dev, OS, and app caches.")
        {
            pathOption, verboseOption, updateCommand,
        };

        root.SetAction((result, ct) => app.InteractiveAsync(BuildOptions(result), ct));
        return root;
    }

    private RunOptions BuildOptions(ParseResult result)
    {
        // The first path is the project-local working directory; all paths feed workspace sweeps.
        var paths = result.GetValue(pathOption) ?? [];
        return new RunOptions
        {
            Verbose = result.GetValue(verboseOption),
            WorkingDirectory = paths is { Length: > 0 } ? paths[0] : Environment.CurrentDirectory,
            ScanRoots = paths,
        };
    }
}

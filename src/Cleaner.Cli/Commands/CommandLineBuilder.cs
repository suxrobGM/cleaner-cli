using System.CommandLine;
using Cleaner.Cli.Application;

namespace Cleaner.Cli.Commands;

/// <summary>
/// Builds the System.CommandLine root and hands control to the interactive menu.
/// </summary>
/// <remarks>
/// Cleaner is a tool a person drives at a terminal, so nothing is ever deleted unattended: running
/// <c>cleaner</c> opens a menu where every action is chosen, previewed, and confirmed. The only
/// flags are the ones the menu can't reasonably ask for — where to look (<c>--path</c>) and how much
/// detail to show (<c>--verbose</c>) — plus the built-in <c>--help</c> and <c>--version</c>.
/// <c>update</c> stays a subcommand because it's the one thing you may need to run before the menu
/// works; it is also reachable from the menu.
/// </remarks>
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
        // One --path flag, repeatable: the first is the primary working dir for single-directory
        // project-local cleaners; all of them are the roots the workspace sweepers recurse into.
        var paths = result.GetValue(pathOption) ?? [];
        return new RunOptions
        {
            Verbose = result.GetValue(verboseOption),
            WorkingDirectory = paths is { Length: > 0 } ? paths[0] : Environment.CurrentDirectory,
            ScanRoots = paths,
        };
    }
}

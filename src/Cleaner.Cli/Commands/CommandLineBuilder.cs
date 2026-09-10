using System.CommandLine;
using Cleaner.Cli.Application;

namespace Cleaner.Cli.Commands;

/// <summary>
/// Builds the System.CommandLine root and hands control to the interactive menu.
/// </summary>
/// <remarks>
/// Nothing is ever deleted unattended, so the only flags are the ones a menu can't ask for: where
/// to look and how much detail to show. <c>update</c> stays a subcommand because you may need it
/// before the menu is useful; it is on the menu too.
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

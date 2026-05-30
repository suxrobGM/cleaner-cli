using System.CommandLine;
using Cleaner.Cli.Application;
using Cleaner.Cli.Rendering;
using Cleaner.Core.Abstractions;
using Spectre.Console;

namespace Cleaner.Cli.Commands;

/// <summary>
/// Builds the System.CommandLine tree and wires each command to <see cref="CleanerApp"/>. Owns the
/// shared options/argument so every command parses them consistently.
/// </summary>
public sealed class CommandLineBuilder(CleanerApp app, IConsoleRenderer renderer)
{
    private readonly Argument<string[]> idsArgument = new("cleaners")
    {
        Description = "Cleaner ids to act on (e.g. nuget npm). Omit with --all or --category.",
        Arity = ArgumentArity.ZeroOrMore,
    };

    private readonly Option<bool> allOption = new("--all", "-a") { Description = "Act on every applicable cleaner." };
    private readonly Option<string?> categoryOption = new("--category", "-c") { Description = "Act on all cleaners in a category." };
    private readonly Option<bool> dryRunOption = new("--dry-run", "-n") { Description = "Preview reclaimable space without deleting." };
    private readonly Option<bool> yesOption = new("--yes", "-y") { Description = "Skip the confirmation prompt." };
    private readonly Option<bool> forceOption = new("--force", "-f") { Description = "Include targets that are otherwise treated cautiously." };
    private readonly Option<string?> pathOption = new("--path", "-p") { Description = "Base directory for project-local cleaners (defaults to cwd)." };
    private readonly Option<bool> checkOption = new("--check") { Description = "Only check for a newer release; don't download or install." };

    public RootCommand Build()
    {
        var listCommand = new Command("list", "List available cleaners and their status.");
        listCommand.SetAction(_ => app.List());

        var scanCommand = new Command("scan", "Report reclaimable space without deleting anything.")
        {
            idsArgument, allOption, categoryOption, pathOption,
        };
        scanCommand.SetAction((result, ct) =>
            TryResolve(result, out var selected)
                ? app.ScanAsync(selected, BuildOptions(result), ct)
                : Task.FromResult(1));

        var cleanCommand = new Command("clean", "Scan, preview, confirm, and delete the selected caches.")
        {
            idsArgument, allOption, categoryOption, dryRunOption, yesOption, forceOption, pathOption,
        };
        cleanCommand.SetAction((result, ct) =>
            TryResolve(result, out var selected)
                ? app.CleanAsync(selected, BuildOptions(result), ct)
                : Task.FromResult(1));

        var updateCommand = new Command("update", "Check for a newer release and install it.")
        {
            checkOption, yesOption,
        };
        updateCommand.SetAction((result, ct) =>
            app.UpdateAsync(result.GetValue(checkOption), result.GetValue(yesOption), ct));

        var root = new RootCommand("Cleaner — reclaim disk space by clearing dev, OS, and app caches.")
        {
            listCommand, scanCommand, cleanCommand, updateCommand,
        };

        // Default (no subcommand) → interactive menu.
        root.SetAction((result, ct) => app.InteractiveAsync(BuildOptions(result), ct));
        return root;
    }

    private RunOptions BuildOptions(ParseResult result) => new()
    {
        DryRun = result.GetValue(dryRunOption),
        Force = result.GetValue(forceOption),
        AssumeYes = result.GetValue(yesOption),
        WorkingDirectory = result.GetValue(pathOption) is { Length: > 0 } p ? p : Environment.CurrentDirectory,
    };

    private bool TryResolve(ParseResult result, out IReadOnlyList<ICleaner> selected)
    {
        var ids = result.GetValue(idsArgument) ?? [];
        var category = result.GetValue(categoryOption);
        var all = result.GetValue(allOption);

        if (!all && string.IsNullOrWhiteSpace(category) && ids.Length == 0)
        {
            renderer.Line("[red]Specify one or more cleaner ids, or --all, or --category <name>.[/]");
            renderer.Line("[grey]Run 'cleaner list' to see available cleaners.[/]");
            selected = [];
            return false;
        }

        selected = app.Resolve(ids, category, all, out var unknown);
        if (unknown.Count > 0)
        {
            renderer.Line($"[red]Unknown cleaner id(s): {string.Join(", ", unknown).EscapeMarkup()}[/]");
            renderer.Line("[grey]Run 'cleaner list' to see available cleaners.[/]");
            return false;
        }

        return true;
    }
}

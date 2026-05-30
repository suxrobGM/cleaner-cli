using System.CommandLine;
using Cleaner.Cli;
using Cleaner.Cli.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

var services = new ServiceCollection();
services.AddCleaner();
using var provider = services.BuildServiceProvider();
var app = provider.GetRequiredService<CleanerApp>();

// Remove any leftover binary backup from a previous self-update (Windows renames the old exe aside).
provider.GetRequiredService<Cleaner.Core.Services.IUpdateService>().CleanupStaleBackup();

// Shared options/arguments.
var idsArgument = new Argument<string[]>("cleaners")
{
    Description = "Cleaner ids to act on (e.g. nuget npm). Omit with --all or --category.",
    Arity = ArgumentArity.ZeroOrMore,
};
var allOption = new Option<bool>("--all", "-a") { Description = "Act on every applicable cleaner." };
var categoryOption = new Option<string?>("--category", "-c") { Description = "Act on all cleaners in a category." };
var dryRunOption = new Option<bool>("--dry-run", "-n") { Description = "Preview reclaimable space without deleting." };
var yesOption = new Option<bool>("--yes", "-y") { Description = "Skip the confirmation prompt." };
var forceOption = new Option<bool>("--force", "-f") { Description = "Include targets that are otherwise treated cautiously." };
var pathOption = new Option<string?>("--path", "-p") { Description = "Base directory for project-local cleaners (defaults to cwd)." };

RunOptions BuildOptions(ParseResult result) => new()
{
    DryRun = result.GetValue(dryRunOption),
    Force = result.GetValue(forceOption),
    AssumeYes = result.GetValue(yesOption),
    WorkingDirectory = result.GetValue(pathOption) is { Length: > 0 } p ? p : Environment.CurrentDirectory,
};

bool TryResolve(ParseResult result, out IReadOnlyList<Cleaner.Core.Abstractions.ICleaner> selected)
{
    var ids = result.GetValue(idsArgument) ?? [];
    var category = result.GetValue(categoryOption);
    var all = result.GetValue(allOption);

    if (!all && string.IsNullOrWhiteSpace(category) && ids.Length == 0)
    {
        AnsiConsole.MarkupLine("[red]Specify one or more cleaner ids, or --all, or --category <name>.[/]");
        AnsiConsole.MarkupLine("[grey]Run 'cleaner list' to see available cleaners.[/]");
        selected = [];
        return false;
    }

    selected = app.Resolve(ids, category, all, out var unknown);
    if (unknown.Count > 0)
    {
        AnsiConsole.MarkupLine($"[red]Unknown cleaner id(s): {string.Join(", ", unknown).EscapeMarkup()}[/]");
        AnsiConsole.MarkupLine("[grey]Run 'cleaner list' to see available cleaners.[/]");
        return false;
    }

    return true;
}

// list
var listCommand = new Command("list", "List available cleaners and their status.");
listCommand.SetAction(_ => app.List());

// scan
var scanCommand = new Command("scan", "Report reclaimable space without deleting anything.")
{
    idsArgument, allOption, categoryOption, pathOption,
};
scanCommand.SetAction((result, ct) =>
    TryResolve(result, out var selected)
        ? app.ScanAsync(selected, BuildOptions(result), ct)
        : Task.FromResult(1));

// clean
var cleanCommand = new Command("clean", "Scan, preview, confirm, and delete the selected caches.")
{
    idsArgument, allOption, categoryOption, dryRunOption, yesOption, forceOption, pathOption,
};
cleanCommand.SetAction((result, ct) =>
    TryResolve(result, out var selected)
        ? app.CleanAsync(selected, BuildOptions(result), ct)
        : Task.FromResult(1));

// update
var checkOption = new Option<bool>("--check") { Description = "Only check for a newer release; don't download or install." };
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

return await root.Parse(args).InvokeAsync();

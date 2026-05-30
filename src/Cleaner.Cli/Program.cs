using System.CommandLine;
using Spectre.Console;

// NOTE: the full command tree (list / scan / clean / interactive) is wired up in a later step.
var root = new RootCommand("Cleaner — reclaim disk space by clearing dev, OS, and app caches.");
root.SetAction(_ =>
{
    AnsiConsole.MarkupLine("[bold]cleaner[/] — bootstrap. Command tree coming soon.");
    return 0;
});

return root.Parse(args).Invoke();

using Cleaner.Core.Abstractions;
using Cleaner.Core.Utils;
using Spectre.Console;

namespace Cleaner.Cli.Rendering;

/// <summary>The prompts: the main menu, the cleaner picker, and the per-folder picker.</summary>
public sealed partial class ConsoleRenderer
{
    internal static IReadOnlyList<(string Label, MainMenuChoice Choice)> MainMenuChoices { get; } =
    [
        ("Preview and clean caches", MainMenuChoice.Clean),
        ("List all cleaners", MainMenuChoice.List),
        ("Check for updates", MainMenuChoice.Update),
        ("Exit", MainMenuChoice.Exit),
    ];

    /// <summary>Up to this many folders read fine flat; beyond it they group by name.</summary>
    private const int GroupThreshold = 8;

    public bool Confirm(string markup, bool defaultValue = false) =>
        IsInteractive ? console.Confirm(markup, defaultValue) : defaultValue;

    public MainMenuChoice PromptMainMenu()
    {
        // SelectionPrompt returns strings, so keep the label-to-choice mapping explicit.
        var prompt = new SelectionPrompt<string>()
            .Title("What would you like to do?")
            .AddChoices(MainMenuChoices.Select(c => c.Label));

        var picked = console.Prompt(prompt);
        return MainMenuChoices.First(c => string.Equals(c.Label, picked, StringComparison.Ordinal)).Choice;
    }

    public IReadOnlyList<ICleaner> PromptSelection(IReadOnlyList<ICleaner> choosable) =>
        PromptTree<ICleaner>(
            "Select what to [green]clean[/]:",
            "[grey](space to toggle, enter to confirm — toggle a group or [bold]All cleaners[/] to take everything under it)[/]",
            (prompt, labels) =>
            {
                // Parent nodes give bulk selection; the Id keeps same-named cleaners apart.
                var all = prompt.AddChoice("All cleaners");
                foreach (var group in ByLayout(choosable, c => c.Category))
                {
                    var groupNode = all.AddChild($"[bold]{group.Key}[/]");
                    foreach (var category in group)
                    {
                        var categoryNode = groupNode.AddChild(category.Key);
                        foreach (var cleaner in category)
                        {
                            var label = $"{cleaner.Name} [grey]({cleaner.Id})[/]";
                            labels[label] = cleaner;
                            categoryNode.AddChild(label);
                        }
                    }
                }
            });

    public IReadOnlyList<string>? PromptFolders(IReadOnlyList<ScanRow> rows) =>
        IsInteractive
            ? PromptTree<string>(
                "Untick anything you want to [green]keep[/]:",
                "[grey](space to toggle, enter to confirm — everything starts ticked)[/]",
                (prompt, labels) =>
                {
                    foreach (var row in rows)
                    {
                        var cleanerNode = prompt.AddChoice(
                            GroupLabel(row.Cleaner.Name, row.Result.TotalBytes, row.Result.Targets.Count));
                        cleanerNode.Select();

                        foreach (var (name, targets) in FolderGroups(row))
                        {
                            var parent = name is { Length: > 0 }
                                ? AddTicked(cleanerNode, GroupLabel(name, targets.Sum(t => t.Bytes), targets.Count))
                                : cleanerNode;

                            foreach (var target in targets)
                            {
                                AddTicked(parent, UniqueLabel(labels, target));
                            }
                        }
                    }
                })
            : null;

    /// <summary>
    /// Runs a multi-select tree and maps what was ticked back to values. <paramref name="build"/>
    /// adds the choices, recording a value for each leaf it wants returned.
    /// </summary>
    private IReadOnlyList<T> PromptTree<T>(
        string title,
        string instructions,
        Action<MultiSelectionPrompt<string>, Dictionary<string, T>> build)
    {
        var prompt = new MultiSelectionPrompt<string>()
            .Title(title)
            .PageSize(20)
            .MoreChoicesText("[grey](move up/down to reveal more)[/]")
            .InstructionsText(instructions);

        // Labels carry markup, so map them back to the value each one stands for. Parent nodes
        // are tickable but record nothing, which is what drops them from the result.
        var labels = new Dictionary<string, T>(StringComparer.Ordinal);
        build(prompt, labels);

        var picked = new List<T>();
        foreach (var label in console.Prompt(prompt))
        {
            if (labels.TryGetValue(label, out var value))
            {
                picked.Add(value);
            }
        }

        return picked;
    }

    /// <summary>Adds a pre-ticked child; <c>AddChild</c> is typed narrower than the nodes really are.</summary>
    private static IMultiSelectionItem<string> AddTicked(IMultiSelectionItem<string> parent, string label) =>
        ((IMultiSelectionItem<string>)parent.AddChild(label)).Select();

    /// <summary>
    /// Folders grouped by name (node_modules, .venv, ...), biggest first at both levels. Stays flat
    /// while there are few folders, or when they all share one name and grouping would say nothing.
    /// </summary>
    private static IEnumerable<(string? Name, IReadOnlyList<CleanupTarget> Targets)> FolderGroups(ScanRow row)
    {
        var targets = row.Result.Targets;
        if (targets.Count <= GroupThreshold)
        {
            return [(null, BySize(targets))];
        }

        var grouped = targets
            .GroupBy(t => string.IsNullOrWhiteSpace(t.Description) ? null : t.Description)
            .ToList();

        return grouped.Count < 2
            ? [(null, BySize(targets))]
            : [.. grouped
                .Select(g => (Name: g.Key, Targets: BySize(g)))
                .OrderByDescending(g => g.Targets.Sum(t => t.Bytes))];
    }

    private static IReadOnlyList<CleanupTarget> BySize(IEnumerable<CleanupTarget> targets) =>
        [.. targets.OrderByDescending(t => t.Bytes)];

    private static string GroupLabel(string name, long bytes, int count) =>
        $"{name.EscapeMarkup()} [grey]({SizeFormatter.Humanize(bytes)}, {count} folder{(count == 1 ? string.Empty : "s")})[/]";

    /// <summary>Suffixed until unique, since two cleaners can report the same folder.</summary>
    private static string UniqueLabel(Dictionary<string, string> labels, CleanupTarget target)
    {
        var label = $"[grey]{SizeFormatter.Humanize(target.Bytes)}[/]  {target.Path.EscapeMarkup()}";
        var unique = label;
        for (var n = 2; !labels.TryAdd(unique, target.Path); n++)
        {
            unique = $"{label} [grey]({n})[/]";
        }

        return unique;
    }
}

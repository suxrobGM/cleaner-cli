using Cleaner.Core.Cleaners;
using Spectre.Console;

namespace Cleaner.Cli.Rendering;

/// <summary>
/// The Spectre.Console implementation of <see cref="IConsoleRenderer"/>. The rest of it is split by
/// concern into the <c>Tables</c>, <c>Prompts</c>, and <c>Progress</c> partials beside this file.
/// </summary>
public sealed partial class ConsoleRenderer(IAnsiConsole console) : IConsoleRenderer
{
    public bool IsInteractive => console.Profile.Capabilities.Interactive;

    public void Line(string markup) => console.MarkupLine(markup);

    public void InteractiveHeader(string version)
    {
        console.Write(new FigletText("Cleaner").Color(Color.Teal));
        console.MarkupLine($"[grey]Reclaim disk space from dev, OS, and app caches.[/] [dim]v{version.EscapeMarkup()}[/]");
        console.WriteLine();
    }

    public void Pause(string markup)
    {
        if (!IsInteractive)
        {
            return;
        }

        console.MarkupLine(markup);
        console.Input.ReadKey(intercept: true);
    }

    /// <summary>Groups items by category and display group, preserving curated order.</summary>
    private static IEnumerable<IGrouping<string, IGrouping<string, T>>> ByLayout<T>(
        IEnumerable<T> items,
        Func<T, string> categoryOf) =>
        items.GroupBy(categoryOf, StringComparer.Ordinal)
            .OrderBy(c => Categories.RankOf(c.Key))
            .GroupBy(c => Categories.GroupOf(c.Key), StringComparer.Ordinal)
            .OrderBy(g => Categories.RankOfGroup(g.Key));
}

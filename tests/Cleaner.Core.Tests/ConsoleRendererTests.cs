using Cleaner.Cli.Rendering;
using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners;
using Spectre.Console;
using Xunit;

namespace Cleaner.Core.Tests;

/// <summary>What the cleaner list actually puts on screen.</summary>
public sealed class ConsoleRendererTests
{
    private sealed class StubCleaner(string id, string category) : ICleaner
    {
        public string Id => id;

        public string Name => id;

        public string Category => category;

        public bool RequiresElevation => false;

        public bool IsApplicable(CleanupContext context) => true;

        public bool IsAvailable(CleanupContext context) => true;

        public Task<ScanResult> ScanAsync(CleanupContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(ScanResult.Empty);

        public Task<CleanResult> CleanAsync(CleanupContext context, IProgress<CleanProgress>? progress = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(CleanResult.Empty);
    }

    private static IAnsiConsole Recording(TextWriter writer) =>
        AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(writer),
        });

    [Fact]
    public void Main_menu_has_one_preview_then_confirm_cache_action()
    {
        var choices = ConsoleRenderer.MainMenuChoices;

        Assert.Equal(("Preview and clean caches", MainMenuChoice.Clean), choices[0]);
        Assert.Single(choices, choice => choice.Choice == MainMenuChoice.Clean);
    }

    [Fact]
    public void PromptFolders_picks_nothing_when_the_console_cannot_prompt()
    {
        var console = Recording(new StringWriter());
        var row = new ScanRow(
            new StubCleaner("npm", Categories.JavaScript),
            new ScanResult([new CleanupTarget("/a", 10), new CleanupTarget("/b", 20)]));

        // Redirected output cannot answer a prompt, so nothing is narrowed.
        Assert.Null(new ConsoleRenderer(console).PromptFolders([row]));
    }

    [Fact]
    public void CleanerList_renders_a_section_per_group_and_category()
    {
        var writer = new StringWriter();
        var console = Recording(writer);
        console.Profile.Width = 200;

        new ConsoleRenderer(console).CleanerList(
        [
            new CleanerListEntry(new StubCleaner("npm", Categories.JavaScript), CleanerStatus.Available),
            new CleanerListEntry(new StubCleaner("temp", Categories.SystemCaches), CleanerStatus.NotFound),
            new CleanerListEntry(new StubCleaner("steam", Categories.DesktopApps), CleanerStatus.NeedsElevation),
        ]);

        var output = writer.ToString();
        Assert.Contains(CategoryGroups.OperatingSystem, output, StringComparison.Ordinal);
        Assert.Contains(CategoryGroups.Development, output, StringComparison.Ordinal);
        Assert.Contains(CategoryGroups.Applications, output, StringComparison.Ordinal);
        Assert.Contains(Categories.JavaScript, output, StringComparison.Ordinal);
        Assert.Contains("3 cleaners across 3 categories in 3 groups.", output, StringComparison.Ordinal);

        Assert.True(
            output.IndexOf("temp", StringComparison.Ordinal) < output.IndexOf("steam", StringComparison.Ordinal));
    }

    [Fact]
    public void SizeTable_totals_the_rows_it_lists()
    {
        var writer = new StringWriter();
        var console = Recording(writer);
        console.Profile.Width = 200;
        var rows = new[]
        {
            new ScanRow(new StubCleaner("npm", Categories.JavaScript), new ScanResult([new CleanupTarget("/a", 1024)])),
            new ScanRow(new StubCleaner("pip", Categories.Python), new ScanResult([new CleanupTarget("/b", 3072)])),
        };

        new ConsoleRenderer(console).SizeTable(rows, "Reclaimable");

        var output = writer.ToString();
        Assert.Contains("Reclaimable", output, StringComparison.Ordinal);
        Assert.Contains("Total", output, StringComparison.Ordinal);
        Assert.Contains("4.0 KB", output, StringComparison.Ordinal);
    }

    [Fact]
    public void CleanSummary_totals_freed_bytes_and_lists_errors()
    {
        var writer = new StringWriter();
        var console = Recording(writer);
        console.Profile.Width = 200;
        var results = new[]
        {
            new CleanRow(new StubCleaner("npm", Categories.JavaScript), new CleanResult(1024, 1, [])),
            new CleanRow(new StubCleaner("pip", Categories.Python), new CleanResult(3072, 1, ["/b: denied"])),
        };

        new ConsoleRenderer(console).CleanSummary(results);

        var output = writer.ToString();
        Assert.Contains("Total", output, StringComparison.Ordinal);
        Assert.Contains("4.0 KB", output, StringComparison.Ordinal);
        Assert.Contains("1 error(s)", output, StringComparison.Ordinal);
        Assert.Contains("/b: denied", output, StringComparison.Ordinal);
    }
}

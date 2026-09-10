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

    [Fact]
    public void CleanerList_renders_a_section_per_group_and_category()
    {
        var writer = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            ColorSystem = ColorSystemSupport.NoColors,
            Out = new AnsiConsoleOutput(writer),
        });
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
}

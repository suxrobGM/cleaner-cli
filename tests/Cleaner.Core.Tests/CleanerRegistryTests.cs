using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners;
using Cleaner.Core.Services;
using Xunit;

namespace Cleaner.Core.Tests;

public sealed class CleanerRegistryTests
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

    private static CleanerRegistry Build() => new(
    [
        new StubCleaner("npm", Categories.JavaScript),
        new StubCleaner("nuget", Categories.Dotnet),
        new StubCleaner("pip", Categories.Python),
        new StubCleaner("poetry", Categories.Python),
    ]);

    [Fact]
    public void Find_is_case_insensitive_and_returns_null_for_unknown()
    {
        var registry = Build();
        Assert.NotNull(registry.Find("NuGet"));
        Assert.Equal("npm", registry.Find("npm")!.Id);
        Assert.Null(registry.Find("does-not-exist"));
    }

    [Fact]
    public void Categories_are_distinct_and_follow_the_display_order()
    {
        // The curated order, not the alphabetical one: package managers precede the languages.
        var registry = Build();
        Assert.Equal([Categories.Dotnet, Categories.JavaScript, Categories.Python], CategoriesOf(registry));
    }

    [Fact]
    public void Categories_outside_the_layout_sort_last_alphabetically()
    {
        var registry = new CleanerRegistry(
        [
            new StubCleaner("zzz", "Zebra tooling"),
            new StubCleaner("aaa", "Alien tooling"),
            new StubCleaner("nuget", Categories.Dotnet),
        ]);

        Assert.Equal([Categories.Dotnet, "Alien tooling", "Zebra tooling"], CategoriesOf(registry));
    }

    /// <summary>The categories in the order the registry hands its cleaners out.</summary>
    private static string[] CategoriesOf(ICleanerRegistry registry) =>
        [.. registry.All.Select(c => c.Category).Distinct(StringComparer.OrdinalIgnoreCase)];

    [Fact]
    public void All_contains_every_cleaner()
    {
        var registry = Build();
        Assert.Equal(4, registry.All.Count);
    }
}

using Cleaner.Core.Abstractions;
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
        new StubCleaner("npm", "JavaScript"),
        new StubCleaner("nuget", "Package managers"),
        new StubCleaner("pip", "Python"),
        new StubCleaner("poetry", "Python"),
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
    public void InCategory_returns_matching_cleaners()
    {
        var registry = Build();
        var python = registry.InCategory("Python");
        Assert.Equal(2, python.Count);
        Assert.All(python, c => Assert.Equal("Python", c.Category));
    }

    [Fact]
    public void Categories_are_distinct_and_sorted()
    {
        var registry = Build();
        Assert.Equal(["JavaScript", "Package managers", "Python"], registry.Categories);
    }

    [Fact]
    public void All_contains_every_cleaner()
    {
        var registry = Build();
        Assert.Equal(4, registry.All.Count);
    }
}

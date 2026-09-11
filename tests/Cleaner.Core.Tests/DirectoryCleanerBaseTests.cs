using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Tests.Fakes;
using Cleaner.Core.Utils;
using Xunit;

namespace Cleaner.Core.Tests;

public sealed class DirectoryCleanerBaseTests
{
    private sealed class TestCleaner(params CleanupPath[] targets) : DirectoryCleanerBase
    {
        public override string Id => "test";

        public override string Name => "Test";

        public override string Category => "Test";

        protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) => targets;
    }

    [Fact]
    public async Task Scan_measures_only_existing_targets()
    {
        var fs = new FakeFileSystem()
            .AddFile("/cache/a.bin", 100)
            .AddFile("/cache/sub/b.bin", 50);
        var cleaner = new TestCleaner(new CleanupPath("/cache"), new CleanupPath("/missing"));

        var result = await cleaner.ScanAsync(TestContext.Create(fs));

        Assert.Equal(1, result.ItemCount);
        Assert.Equal(150, result.TotalBytes);
    }

    [Fact]
    public async Task Clean_deletes_targets_and_reports_freed()
    {
        var fs = new FakeFileSystem().AddFile("/cache/a.bin", 100);
        var cleaner = new TestCleaner(new CleanupPath("/cache"));

        var result = await cleaner.CleanAsync(TestContext.Create(fs));

        Assert.Equal(100, result.BytesFreed);
        Assert.Equal(1, result.ItemsRemoved);
        Assert.False(result.HasErrors);
        Assert.False(fs.DirectoryExists("/cache"));
    }

    [Fact]
    public async Task Clean_clear_contents_keeps_the_directory()
    {
        var fs = new FakeFileSystem().AddFile("/cache/a.bin", 100).AddDirectory("/cache");
        var cleaner = new TestCleaner(new CleanupPath("/cache", DeleteMode.ClearContents));

        var result = await cleaner.CleanAsync(TestContext.Create(fs));

        Assert.Equal(100, result.BytesFreed);
        Assert.True(fs.DirectoryExists("/cache"));
        Assert.False(fs.FileExists("/cache/a.bin"));
    }

    [Fact]
    public async Task DryRun_reports_but_does_not_delete()
    {
        var fs = new FakeFileSystem().AddFile("/cache/a.bin", 100);
        var cleaner = new TestCleaner(new CleanupPath("/cache"));

        var result = await cleaner.CleanAsync(TestContext.Create(fs, dryRun: true));

        Assert.Equal(100, result.BytesFreed);
        Assert.True(fs.DirectoryExists("/cache"));
    }

    [Fact]
    public async Task Clean_without_a_selection_deletes_every_target()
    {
        var fs = ThreeCaches();
        var cleaner = new TestCleaner(new CleanupPath("/a"), new CleanupPath("/b"), new CleanupPath("/c"));

        var result = await cleaner.CleanAsync(TestContext.Create(fs));

        Assert.Equal(3, result.ItemsRemoved);
        Assert.Equal(60, result.BytesFreed);
    }

    [Fact]
    public async Task Clean_deletes_only_the_selected_folders()
    {
        var fs = ThreeCaches();
        var cleaner = new TestCleaner(new CleanupPath("/a"), new CleanupPath("/b"), new CleanupPath("/c"));
        var context = TestContext.Create(fs, selectedPaths: PathComparison.CreateSet(["/b"], isLinux: false));

        var result = await cleaner.CleanAsync(context);

        Assert.Equal(1, result.ItemsRemoved);
        Assert.Equal(20, result.BytesFreed);
        Assert.True(fs.DirectoryExists("/a"));
        Assert.False(fs.DirectoryExists("/b"));
        Assert.True(fs.DirectoryExists("/c"));
    }

    [Fact]
    public async Task Clean_with_an_empty_selection_deletes_nothing()
    {
        var fs = ThreeCaches();
        var cleaner = new TestCleaner(new CleanupPath("/a"), new CleanupPath("/b"));
        var context = TestContext.Create(fs, selectedPaths: PathComparison.CreateSet([], isLinux: false));

        var result = await cleaner.CleanAsync(context);

        Assert.Equal(0, result.ItemsRemoved);
        Assert.False(result.HasErrors);
        Assert.True(fs.DirectoryExists("/a"));
    }

    [Fact]
    public async Task Selection_matches_a_folder_spelled_with_either_separator()
    {
        var fs = ThreeCaches();
        var target = $"{Path.DirectorySeparatorChar}a";
        var selected = $"{Path.AltDirectorySeparatorChar}a";
        var cleaner = new TestCleaner(new CleanupPath(target));
        var context = TestContext.Create(fs, selectedPaths: PathComparison.CreateSet([selected], isLinux: false));

        var result = await cleaner.CleanAsync(context);

        Assert.Equal(1, result.ItemsRemoved);
    }

    [Fact]
    public async Task Scan_reports_only_the_selected_folders()
    {
        var fs = ThreeCaches();
        var cleaner = new TestCleaner(new CleanupPath("/a"), new CleanupPath("/b"), new CleanupPath("/c"));
        var context = TestContext.Create(fs, selectedPaths: PathComparison.CreateSet(["/a", "/c"], isLinux: false));

        var result = await cleaner.ScanAsync(context);

        Assert.Equal(2, result.ItemCount);
        Assert.Equal(40, result.TotalBytes);
    }

    [Fact]
    public async Task Clean_captures_errors_and_continues()
    {
        var fs = new FakeFileSystem().AddFile("/bad/a.bin", 100).AddFile("/good/b.bin", 30);
        fs.ThrowOnDelete.Add("/bad");
        var cleaner = new TestCleaner(new CleanupPath("/bad"), new CleanupPath("/good"));

        var result = await cleaner.CleanAsync(TestContext.Create(fs));

        Assert.Single(result.Errors);
        Assert.Equal(30, result.BytesFreed);
        Assert.True(fs.DirectoryExists("/bad"));
        Assert.False(fs.DirectoryExists("/good"));
    }

    private static FakeFileSystem ThreeCaches() => new FakeFileSystem()
        .AddFile("/a/x.bin", 10)
        .AddFile("/b/x.bin", 20)
        .AddFile("/c/x.bin", 30);
}

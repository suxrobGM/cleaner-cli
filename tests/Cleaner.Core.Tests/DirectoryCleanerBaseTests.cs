using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Tests.Fakes;
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
}

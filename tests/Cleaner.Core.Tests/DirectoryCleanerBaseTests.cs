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

        Assert.Single(result.Targets);
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
    public async Task Clean_deletes_only_the_approved_targets()
    {
        var fs = ThreeCaches();
        var cleaner = new TestCleaner(new CleanupPath("/a"), new CleanupPath("/b"), new CleanupPath("/c"));
        var context = TestContext.Create(fs);
        var scan = await cleaner.ScanAsync(context);

        var result = await cleaner.CleanAsync(context, [.. scan.Targets.Where(t => t.Path == "/b")]);

        Assert.Equal(1, result.ItemsRemoved);
        Assert.Equal(20, result.BytesFreed);
        Assert.True(fs.DirectoryExists("/a"));
        Assert.False(fs.DirectoryExists("/b"));
        Assert.True(fs.DirectoryExists("/c"));
    }

    [Fact]
    public async Task Clean_never_rediscovers_targets_that_appeared_after_the_scan()
    {
        var fs = new FakeFileSystem().AddFile("/a/x.bin", 10);
        var cleaner = new TestCleaner(new CleanupPath("/a"), new CleanupPath("/b"));
        var context = TestContext.Create(fs);
        var scan = await cleaner.ScanAsync(context);
        fs.AddFile("/b/new.bin", 99);

        var result = await cleaner.CleanAsync(context, scan.Targets);

        Assert.Equal(10, result.BytesFreed);
        Assert.True(fs.DirectoryExists("/b"));
    }

    [Fact]
    public async Task Clean_with_no_approved_targets_deletes_nothing()
    {
        var fs = ThreeCaches();
        var cleaner = new TestCleaner(new CleanupPath("/a"), new CleanupPath("/b"));

        var result = await cleaner.CleanAsync(TestContext.Create(fs), []);

        Assert.Equal(0, result.ItemsRemoved);
        Assert.False(result.HasErrors);
        Assert.True(fs.DirectoryExists("/a"));
    }

    [Fact]
    public async Task A_locked_folder_is_skipped_whole_and_the_rest_continues()
    {
        var fs = new FakeFileSystem()
            .AddFile("/bad/a.bin", 100)
            .AddFile("/bad/b.bin", 100)
            .AddFile("/good/b.bin", 30);
        fs.InUse.Add("/bad/a.bin");
        var cleaner = new TestCleaner(new CleanupPath("/bad"), new CleanupPath("/good"));

        var result = await cleaner.CleanAsync(TestContext.Create(fs));

        Assert.False(result.HasErrors);
        Assert.Equal(1, result.SkippedCount);
        Assert.Equal(30, result.BytesFreed);
        Assert.True(fs.FileExists("/bad/a.bin"));
        Assert.True(fs.FileExists("/bad/b.bin"));
        Assert.False(fs.DirectoryExists("/good"));
    }

    [Fact]
    public async Task A_delete_that_fails_after_staging_reports_the_leftover()
    {
        var fs = new FakeFileSystem().AddFile("/bad/a.bin", 100);
        var cleaner = new TestCleaner(new CleanupPath("/bad"));
        var context = TestContext.Create(fs);
        var scan = await cleaner.ScanAsync(context);
        fs.FailDeletesUnder.Add("/bad.cleaner-deleting-");

        var result = await cleaner.CleanAsync(context, scan.Targets);

        Assert.Single(result.Errors);
        Assert.Contains(".cleaner-deleting-", result.Errors[0]);
        Assert.False(fs.DirectoryExists("/bad"));
        Assert.Contains(".cleaner-deleting-", Assert.Single(fs.ReadAllLines("/home/test/.cleaner/pending-deletes.txt")));
    }

    [Fact]
    public async Task Clear_contents_skips_protected_children_and_clears_the_rest()
    {
        var env = new FakeEnvironment { PathDirectories = ["/tmpcache/tool/bin"] };
        var fs = new FakeFileSystem()
            .AddFile("/tmpcache/tool/bin/tool", 5)
            .AddFile("/tmpcache/junk/x.bin", 50);
        var cleaner = new TestCleaner(new CleanupPath("/tmpcache", DeleteMode.ClearContents));
        var context = TestContext.Create(fs, env);

        var scan = await cleaner.ScanAsync(context);
        var result = await cleaner.CleanAsync(context, scan.Targets);

        Assert.Equal(50, scan.TotalBytes);
        Assert.Contains(scan.Notes, n => n.Contains("protected"));
        Assert.Equal(50, result.BytesFreed);
        Assert.True(fs.FileExists("/tmpcache/tool/bin/tool"));
        Assert.False(fs.DirectoryExists("/tmpcache/junk"));
    }

    [Fact]
    public async Task Protected_targets_are_neither_offered_nor_deleted()
    {
        var env = new FakeEnvironment { PathDirectories = ["/opt-tools/bin"] };
        var fs = new FakeFileSystem().AddFile("/opt-tools/bin/tool", 5).AddFile("/cache/x", 7);
        var cleaner = new TestCleaner(new CleanupPath("/opt-tools"), new CleanupPath("/cache"));
        var context = TestContext.Create(fs, env);

        var scan = await cleaner.ScanAsync(context);

        // Even if a caller approves the protected folder anyway, the remover refuses it.
        var forced = await cleaner.CleanAsync(context, [new CleanupTarget("/opt-tools", 5, DeleteMode.DeleteDirectory)]);

        Assert.Equal(["/cache"], scan.Targets.Select(t => t.Path));
        Assert.Equal(1, forced.SkippedCount);
        Assert.True(fs.FileExists("/opt-tools/bin/tool"));
    }

    private static FakeFileSystem ThreeCaches() => new FakeFileSystem()
        .AddFile("/a/x.bin", 10)
        .AddFile("/b/x.bin", 20)
        .AddFile("/c/x.bin", 30);
}

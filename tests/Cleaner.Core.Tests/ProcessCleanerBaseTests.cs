using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Services;
using Cleaner.Core.Tests.Fakes;
using Xunit;

namespace Cleaner.Core.Tests;

public sealed class ProcessCleanerBaseTests
{
    private sealed class TestProcessCleaner(string executable, params CleanupPath[] targets) : ProcessCleanerBase
    {
        public override string Id => "test-proc";

        public override string Name => "Test Proc";

        public override string Category => "Test";

        protected override string Executable => executable;

        protected override IReadOnlyList<string> CleanArguments => ["clean"];

        protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) => targets;
    }

    [Fact]
    public async Task Runs_command_when_tool_is_available()
    {
        var fs = new FakeFileSystem().AddFile("/cache/a.bin", 200);
        var runner = new FakeProcessRunner().WithAvailable("tool");
        runner.OnRun = () => fs.DeleteDirectory("/cache");
        var cleaner = new TestProcessCleaner("tool", new CleanupPath("/cache"));

        var result = await cleaner.CleanAsync(TestContext.Create(fs, processRunner: runner));

        Assert.Single(runner.Invocations);
        Assert.Equal("tool", runner.Invocations[0].Executable);
        Assert.Equal(200, result.BytesFreed);
        Assert.False(result.HasErrors);
    }

    [Fact]
    public async Task Falls_back_to_directory_delete_when_tool_missing()
    {
        var fs = new FakeFileSystem().AddFile("/cache/a.bin", 200);
        var runner = new FakeProcessRunner();
        var cleaner = new TestProcessCleaner("tool", new CleanupPath("/cache"));

        var result = await cleaner.CleanAsync(TestContext.Create(fs, processRunner: runner));

        Assert.Empty(runner.Invocations);
        Assert.Equal(200, result.BytesFreed);
        Assert.False(fs.DirectoryExists("/cache"));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Scan_reports_whether_the_tool_can_run_without_folders(bool installed)
    {
        var runner = installed ? new FakeProcessRunner().WithAvailable("tool") : new FakeProcessRunner();
        var cleaner = new TestProcessCleaner("tool", new CleanupPath("/cache"));

        var scan = await cleaner.ScanAsync(TestContext.Create(new FakeFileSystem(), processRunner: runner));

        Assert.Empty(scan.Targets);
        Assert.Equal(installed, scan.HasWork);
    }

    [Fact]
    public void Command_driven_cleaners_do_not_offer_partial_selection()
    {
        Assert.False(new TestProcessCleaner("tool").SupportsPartialSelection);
    }

    [Fact]
    public async Task A_tool_whose_cache_is_protected_still_runs_its_own_cleanup()
    {
        // Scoop's uv caches under scoop\persist: never deleted directly, but `uv cache clean` still runs.
        var env = new FakeEnvironment().SetVariable("SCOOP", "/home/test/scoop");
        var fs = new FakeFileSystem().AddFile("/home/test/scoop/persist/uv/cache/x.whl", 200);
        var runner = new FakeProcessRunner().WithAvailable("tool");
        var cleaner = new TestProcessCleaner("tool", new CleanupPath("/home/test/scoop/persist/uv/cache"));
        var context = TestContext.Create(fs, env, runner);

        var scan = await cleaner.ScanAsync(context);
        var result = await cleaner.CleanAsync(context, scan.Targets);

        Assert.Empty(scan.Targets);
        Assert.True(cleaner.IsAvailable(context));
        Assert.Single(runner.Invocations);
        Assert.False(result.HasErrors);
    }

    [Fact]
    public async Task Without_the_tool_a_protected_cache_makes_the_cleaner_unavailable()
    {
        var env = new FakeEnvironment().SetVariable("SCOOP", "/home/test/scoop");
        var fs = new FakeFileSystem().AddFile("/home/test/scoop/persist/uv/cache/x.whl", 200);
        var cleaner = new TestProcessCleaner("tool", new CleanupPath("/home/test/scoop/persist/uv/cache"));
        var context = TestContext.Create(fs, env);

        Assert.False(cleaner.IsAvailable(context));
        Assert.Empty((await cleaner.ScanAsync(context)).Targets);
    }

    [Fact]
    public async Task DryRun_neither_runs_nor_deletes()
    {
        var fs = new FakeFileSystem().AddFile("/cache/a.bin", 200);
        var runner = new FakeProcessRunner().WithAvailable("tool");
        var cleaner = new TestProcessCleaner("tool", new CleanupPath("/cache"));

        var result = await cleaner.CleanAsync(TestContext.Create(fs, processRunner: runner, dryRun: true));

        Assert.Empty(runner.Invocations);
        Assert.True(fs.DirectoryExists("/cache"));
        Assert.Equal(200, result.BytesFreed);
    }

    [Fact]
    public async Task Command_failure_is_reported_and_deletes_nothing()
    {
        var fs = new FakeFileSystem().AddFile("/cache/a.bin", 200);
        var runner = new FakeProcessRunner().WithAvailable("tool");
        runner.Result = new ProcessResult(1, string.Empty, "boom");
        var cleaner = new TestProcessCleaner("tool", new CleanupPath("/cache"));

        var result = await cleaner.CleanAsync(TestContext.Create(fs, processRunner: runner));

        // The tool knows which of its folders are safe; when it fails, Cleaner doesn't guess.
        Assert.Single(runner.Invocations);
        Assert.True(result.HasErrors);
        Assert.Contains("boom", result.Errors[0]);
        Assert.Equal(0, result.BytesFreed);
        Assert.True(fs.FileExists("/cache/a.bin"));
    }
}

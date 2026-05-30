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
        runner.OnRun = () => fs.DeleteContents("/cache"); // simulate the command clearing the cache
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
        var runner = new FakeProcessRunner(); // "tool" not available
        var cleaner = new TestProcessCleaner("tool", new CleanupPath("/cache"));

        var result = await cleaner.CleanAsync(TestContext.Create(fs, processRunner: runner));

        Assert.Empty(runner.Invocations);
        Assert.Equal(200, result.BytesFreed);
        Assert.False(fs.DirectoryExists("/cache"));
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
        Assert.Equal(200, result.BytesFreed); // reported as reclaimable
    }

    [Fact]
    public async Task Command_failure_falls_back_and_records_error()
    {
        var fs = new FakeFileSystem().AddFile("/cache/a.bin", 200);
        var runner = new FakeProcessRunner().WithAvailable("tool");
        runner.Result = new ProcessResult(1, string.Empty, "boom"); // command fails, no OnRun
        var cleaner = new TestProcessCleaner("tool", new CleanupPath("/cache"));

        var result = await cleaner.CleanAsync(TestContext.Create(fs, processRunner: runner));

        Assert.Single(runner.Invocations);
        Assert.True(result.HasErrors);
        Assert.Equal(200, result.BytesFreed); // fallback deletion succeeded
        Assert.False(fs.DirectoryExists("/cache"));
    }
}

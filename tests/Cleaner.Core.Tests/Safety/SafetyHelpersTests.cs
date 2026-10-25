using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Os;
using Cleaner.Core.Services;
using Cleaner.Core.Tests.Fakes;
using Cleaner.Core.Utils;
using Xunit;

namespace Cleaner.Core.Tests.Safety;

public sealed class SafetyHelpersTests
{
    [Theory]
    [InlineData(@"C:\Users\test\.cargo\bin", @"C:\Users\test\.cargo", true)]
    [InlineData(@"C:\Users\test\.cargo", @"C:\Users\test\.cargo\", true)]
    [InlineData(@"C:/users/TEST/.cargo/bin", @"C:\Users\test\.cargo", true)]
    [InlineData(@"C:\Users\test\.cargo-old", @"C:\Users\test\.cargo", false)]
    [InlineData(@"C:\Windows.old", @"C:\Windows", false)]
    [InlineData(@"C:\anything", @"C:\", true)]
    [InlineData("/home/test", "/", true)]
    [InlineData("/home", "/home/test", false)]
    public void IsSameOrUnder_is_plain_string_logic_on_any_host(string path, string root, bool expected) =>
        Assert.Equal(expected, PathComparison.IsSameOrUnder(path, root, isLinux: false));

    [Theory]
    [InlineData("/", true)]
    [InlineData(@"C:\", true)]
    [InlineData("C:", true)]
    [InlineData(@"\\server\share", true)]
    [InlineData(@"C:\Users", false)]
    [InlineData("/home", false)]
    public void IsRoot_recognises_volume_and_filesystem_roots(string path, bool expected) =>
        Assert.Equal(expected, PathComparison.IsRoot(path));

    [Theory]
    [InlineData(@"C:\x", true)]
    [InlineData("/x", true)]
    [InlineData(@"\\server\share", true)]
    [InlineData(@"x\y", false)]
    [InlineData(".", false)]
    [InlineData("", false)]
    public void IsRooted_works_for_both_platforms_on_any_host(string path, bool expected) =>
        Assert.Equal(expected, PathComparison.IsRooted(path));

    [Fact]
    public void Parent_keeps_the_separator_style_and_stops_at_roots()
    {
        Assert.Equal(@"C:\Users", PathComparison.Parent(@"C:\Users\test"));
        Assert.Equal(@"C:\", PathComparison.Parent(@"C:\Users"));
        Assert.Null(PathComparison.Parent(@"C:\"));
        Assert.Equal("/", PathComparison.Parent("/home"));
        Assert.Equal("test", PathComparison.Leaf(@"C:\Users\test\"));
    }

    [Fact]
    public async Task Tool_health_check_reports_a_tool_that_stopped_working_and_lost_PATH_files()
    {
        var fs = new FakeFileSystem().AddFile("/tools/bin/node", 10).AddFile("/tools/bin/npm", 10);
        var runner = new FakeProcessRunner().WithAvailable("node", "git");
        var nodeWorks = true;
        runner.Respond = (exe, _) => exe == "node" && !nodeWorks ? new ProcessResult(1, "", "missing module") : null;

        var before = await ToolHealthCheck.CaptureAsync(runner, fs, ["/tools/bin"], CancellationToken.None);
        nodeWorks = false;
        fs.DeleteFile("/tools/bin/npm");
        var after = await ToolHealthCheck.CaptureAsync(runner, fs, ["/tools/bin"], CancellationToken.None);

        var problems = ToolHealthCheck.Regressions(before, after);

        Assert.Contains(problems, p => p.Contains("'node'"));
        Assert.Contains(problems, p => p.Contains("/tools/bin") && p.Contains("npm"));
        Assert.DoesNotContain(problems, p => p.Contains("'git'"));
        Assert.Contains(runner.Invocations, i => i.Executable == "node" && i.Timeout is not null);
    }

    [Fact]
    public async Task Tool_health_check_is_quiet_when_nothing_changed()
    {
        var fs = new FakeFileSystem().AddFile("/tools/bin/node", 10);
        var runner = new FakeProcessRunner().WithAvailable("node");

        var before = await ToolHealthCheck.CaptureAsync(runner, fs, ["/tools/bin", "/missing"], CancellationToken.None);
        var after = await ToolHealthCheck.CaptureAsync(runner, fs, ["/tools/bin", "/missing"], CancellationToken.None);

        Assert.Empty(ToolHealthCheck.Regressions(before, after));
    }

    [Fact]
    public async Task Leftover_cleaner_finishes_recorded_staged_deletes_and_nothing_else()
    {
        var fs = new FakeFileSystem()
            .AddFile("/work/bin.cleaner-deleting-ab12cd34/app.dll", 40)
            .AddFile("/work/keep/important.txt", 1)
            .AddFile("/home/test/.cleaner/pending-deletes.txt",
                "/work/bin.cleaner-deleting-ab12cd34\n/work/keep\n/work/gone.cleaner-deleting-00000000\n");
        var cleaner = new StagedLeftoverCleaner();
        var context = TestContext.Create(fs);

        var scan = await cleaner.ScanAsync(context);
        var result = await cleaner.CleanAsync(context, scan.Targets);

        Assert.Equal(["/work/bin.cleaner-deleting-ab12cd34"], scan.Targets.Select(t => t.Path));
        Assert.Equal(40, result.BytesFreed);
        Assert.True(fs.FileExists("/work/keep/important.txt"));
        Assert.DoesNotContain(fs.ReadAllLines("/home/test/.cleaner/pending-deletes.txt"), l => l.Length > 0);
    }

    [Fact]
    public async Task Trash_empties_each_readable_user_bin_inside_it()
    {
        var env = FakeEnvironment.Windows();
        env.FixedDriveRoots = [@"C:\"];
        var fs = new FakeFileSystem()
            .AddFile(@"C:\$Recycle.Bin\S-1-5-21-1\$RABC.zip", 500)
            .AddFile(@"C:\$Recycle.Bin\S-1-5-21-1\$IABC.zip", 1);

        var result = await new TrashCleaner().CleanAsync(TestContext.Create(fs, env));

        Assert.Equal(501, result.BytesFreed);
        Assert.True(fs.DirectoryExists(@"C:\$Recycle.Bin\S-1-5-21-1"));
    }

    [Fact]
    public async Task Every_removal_and_skip_is_logged_with_the_cleaner_and_path()
    {
        var env = new FakeEnvironment { PathDirectories = ["/cache/tool/bin"] };
        var fs = new FakeFileSystem().AddFile("/cache/old/x", 5).AddFile("/cache/tool/bin/t", 1);
        var logger = new NullAppLogger();
        var context = TestContext.Create(fs, env, logger: logger);
        var cleaner = new LoggedCleaner();

        await cleaner.CleanAsync(context, [new CleanupTarget("/cache", 6, DeleteMode.ClearContents)]);

        Assert.Contains(logger.Lines, l => l.StartsWith("[logged] removed") && l.Contains("old"));
        Assert.Contains(logger.Lines, l => l.StartsWith("[logged] skipped") && l.Contains("tool") && l.Contains("PATH"));
    }

    private sealed class LoggedCleaner : Cleaners.Base.DirectoryCleanerBase
    {
        public override string Id => "logged";

        public override string Name => "Logged";

        public override string Category => "Test";
    }
}

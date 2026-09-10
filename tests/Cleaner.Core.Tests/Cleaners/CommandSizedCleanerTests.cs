using Cleaner.Core.Cleaners.DevTools;
using Cleaner.Core.Cleaners.Os;
using Cleaner.Core.Services;
using Cleaner.Core.Tests.Fakes;
using Xunit;

namespace Cleaner.Core.Tests;

/// <summary>
/// Cleaners with nothing measurable on disk, which get their numbers from the tool they drive. The
/// point of each test is that a real size reaches the preview instead of "unknown until it runs".
/// </summary>
public sealed class CommandSizedCleanerTests
{
    private const string DockerDf = """
        1.2GB|800MB (66%)
        150MB|150MB (100%)
        2GB|1.5GB (75%)
        """;

    private const string DismReport = """
        Component Store (WinSxS) directory : 8.15 GB
        Actual Size of Component Store : 6.50 GB
        Shared with Windows : 4.00 GB
        Backups and Disabled Features : 2.00 GB
        Cache and Temporary Data : 500.00 MB
        Date of Last Cleanup : 2026-01-01
        Number of Reclaimable Packages : 12
        Component Store Cleanup Recommended : Yes
        """;

    [Fact]
    public async Task DockerCleaner_estimates_from_the_reclaimable_column()
    {
        var runner = new FakeProcessRunner().WithAvailable("docker");
        runner.Result = new ProcessResult(0, DockerDf, string.Empty);

        var scan = await new DockerCleaner().ScanAsync(TestContext.Create(new FakeFileSystem(), processRunner: runner));

        // 800 MB + 150 MB + 1.5 GB, in the decimal units Docker prints.
        Assert.Equal(800_000_000L + 150_000_000L + 1_500_000_000L, scan.TotalBytes);
        Assert.Equal(["system", "df", "--format", "{{.Size}}|{{.Reclaimable}}"], runner.Invocations[0].Arguments);
    }

    [Fact]
    public async Task DockerCleaner_reports_nothing_when_the_daemon_is_unreachable()
    {
        var runner = new FakeProcessRunner().WithAvailable("docker");
        runner.Result = new ProcessResult(1, string.Empty, "cannot connect to the Docker daemon");

        var scan = await new DockerCleaner().ScanAsync(TestContext.Create(new FakeFileSystem(), processRunner: runner));

        // Nothing measurable beats a wrong number: the UI labels the row instead.
        Assert.Equal(0, scan.TotalBytes);
    }

    [Fact]
    public async Task DockerCleaner_reports_the_drop_in_held_bytes_as_freed()
    {
        var runner = new FakeProcessRunner().WithAvailable("docker");
        var pruned = false;
        runner.Respond = (_, arguments) =>
        {
            if (arguments[0] != "system" || arguments[1] != "df")
            {
                pruned = true;
                return new ProcessResult(0, string.Empty, string.Empty);
            }

            return new ProcessResult(0, pruned ? "400MB|0B (0%)" : "1.2GB|800MB (66%)", string.Empty);
        };

        var result = await new DockerCleaner().CleanAsync(TestContext.Create(new FakeFileSystem(), processRunner: runner));

        Assert.Equal(800_000_000, result.BytesFreed);
        Assert.Contains(runner.Invocations, i => i.Arguments.Contains("prune"));
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task DockerVhdxCleaner_estimates_the_slack_between_the_disk_and_what_docker_holds()
    {
        var fs = new FakeFileSystem().AddFile(@"C:\Users\test\AppData\Local\Docker\wsl\disk\docker_data.vhdx", 10_000_000_000);
        var runner = new FakeProcessRunner().WithAvailable("docker");
        runner.Result = new ProcessResult(0, "4GB|1GB (25%)", string.Empty);

        var scan = await new DockerVhdxCleaner().ScanAsync(TestContext.Create(fs, FakeEnvironment.Windows(), runner));

        Assert.Equal(10_000_000_000L - 4_000_000_000L, scan.TotalBytes);
    }

    [Fact]
    public async Task DockerVhdxCleaner_estimates_nothing_without_the_docker_cli()
    {
        var fs = new FakeFileSystem().AddFile(@"C:\Users\test\AppData\Local\Docker\wsl\disk\docker_data.vhdx", 10_000_000_000);
        var runner = new FakeProcessRunner().WithAvailable("wsl");

        var scan = await new DockerVhdxCleaner().ScanAsync(TestContext.Create(fs, FakeEnvironment.Windows(), runner));

        Assert.Equal(0, scan.TotalBytes);
        Assert.Empty(runner.Invocations);
    }

    [Fact]
    public async Task WinSxSCleaner_estimates_the_removable_part_of_the_component_store()
    {
        var environment = FakeEnvironment.Windows();
        environment.IsElevated = true;
        var runner = new FakeProcessRunner().WithAvailable("dism");
        runner.Result = new ProcessResult(0, DismReport, string.Empty);

        var scan = await new WinSxSCleaner().ScanAsync(TestContext.Create(new FakeFileSystem(), environment, runner));

        // Backups and disabled features plus the servicing scratch — never the shared components.
        Assert.Equal((2L * 1024 * 1024 * 1024) + (500L * 1024 * 1024), scan.TotalBytes);
        Assert.Contains("/AnalyzeComponentStore", runner.Invocations[0].Arguments);
    }

    [Fact]
    public async Task WinSxSCleaner_does_not_analyze_without_elevation()
    {
        // The analysis needs admin just as the cleanup does, so asking would only fail slowly.
        var runner = new FakeProcessRunner().WithAvailable("dism");
        runner.Result = new ProcessResult(0, DismReport, string.Empty);

        var scan = await new WinSxSCleaner().ScanAsync(TestContext.Create(new FakeFileSystem(), FakeEnvironment.Windows(), runner));

        Assert.Equal(0, scan.TotalBytes);
        Assert.Empty(runner.Invocations);
    }

    [Fact]
    public async Task WinSxSCleaner_reports_the_drop_in_store_size_as_freed()
    {
        var environment = FakeEnvironment.Windows();
        environment.IsElevated = true;
        var runner = new FakeProcessRunner().WithAvailable("dism");
        var cleaned = false;
        runner.Respond = (_, arguments) =>
        {
            if (arguments.Contains("/StartComponentCleanup"))
            {
                cleaned = true;
                return new ProcessResult(0, string.Empty, string.Empty);
            }

            return new ProcessResult(
                0,
                cleaned ? "Actual Size of Component Store : 4.50 GB" : "Actual Size of Component Store : 6.50 GB",
                string.Empty);
        };

        var result = await new WinSxSCleaner().CleanAsync(TestContext.Create(new FakeFileSystem(), environment, runner));

        Assert.Equal(2L * 1024 * 1024 * 1024, result.BytesFreed);
        Assert.Empty(result.Errors);
    }
}

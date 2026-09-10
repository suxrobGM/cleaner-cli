using Cleaner.Core.Cleaners.DevTools;
using Cleaner.Core.Cleaners.Os;
using Cleaner.Core.Services;
using Cleaner.Core.Tests.Fakes;
using Xunit;

namespace Cleaner.Core.Tests;

/// <summary>Tests cleaners whose reclaimable size comes from an external tool.</summary>
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

        var scan = await new DockerCleaner().ScanAsync(TestContext.Create(processRunner: runner));

        Assert.Equal(800_000_000L + 150_000_000L + 1_500_000_000L, scan.TotalBytes);
        Assert.Equal(["system", "df", "--format", "{{.Size}}|{{.Reclaimable}}"], runner.Invocations[0].Arguments);
    }

    [Fact]
    public async Task DockerCleaner_reports_nothing_when_the_daemon_is_unreachable()
    {
        var runner = new FakeProcessRunner().WithAvailable("docker");
        runner.Result = new ProcessResult(1, string.Empty, "cannot connect to the Docker daemon");
        var context = TestContext.Create(processRunner: runner);
        var cleaner = new DockerCleaner();

        var scan = await cleaner.ScanAsync(context);

        Assert.Equal(0, scan.TotalBytes);
        Assert.True(scan.ToolUnavailable);
    }

    [Fact]
    public async Task DockerCleaner_reuses_the_scan_to_report_freed_bytes()
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

        var context = TestContext.Create(processRunner: runner);
        var cleaner = new DockerCleaner();
        await cleaner.ScanAsync(context);
        var result = await cleaner.CleanAsync(context);

        Assert.Equal(800_000_000, result.BytesFreed);
        Assert.Contains(runner.Invocations, i => i.Arguments.Contains("prune"));
        Assert.Equal(2, runner.Invocations.Count(i => i.Arguments.Contains("df")));
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

        var scan = await new WinSxSCleaner().ScanAsync(TestContext.Create(environment: environment, processRunner: runner));

        // Shared components are not reclaimable.
        Assert.Equal((2L * 1024 * 1024 * 1024) + (500L * 1024 * 1024), scan.TotalBytes);
        Assert.Contains("/AnalyzeComponentStore", runner.Invocations[0].Arguments);
    }

    [Fact]
    public async Task WinSxSCleaner_does_not_analyze_without_elevation()
    {
        var runner = new FakeProcessRunner().WithAvailable("dism");
        runner.Result = new ProcessResult(0, DismReport, string.Empty);

        var scan = await new WinSxSCleaner().ScanAsync(
            TestContext.Create(environment: FakeEnvironment.Windows(), processRunner: runner));

        Assert.Equal(0, scan.TotalBytes);
        Assert.Empty(runner.Invocations);
    }

    [Fact]
    public async Task WinSxSCleaner_reuses_the_scan_to_report_freed_bytes()
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

            return new ProcessResult(0, cleaned ? DismReport.Replace("6.50 GB", "4.50 GB") : DismReport, string.Empty);
        };

        var context = TestContext.Create(environment: environment, processRunner: runner);
        var cleaner = new WinSxSCleaner();
        await cleaner.ScanAsync(context);
        var result = await cleaner.CleanAsync(context);

        Assert.Equal(2L * 1024 * 1024 * 1024, result.BytesFreed);
        Assert.Contains(runner.Invocations, i => i.Arguments.Contains("/StartComponentCleanup"));
        Assert.Equal(2, runner.Invocations.Count(i => i.Arguments.Contains("/AnalyzeComponentStore")));
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task WinSxSCleaner_measures_afresh_when_the_scan_belonged_to_an_earlier_run()
    {
        var environment = FakeEnvironment.Windows();
        environment.IsElevated = true;
        var runner = new FakeProcessRunner().WithAvailable("dism");
        runner.Result = new ProcessResult(0, DismReport, string.Empty);

        var fileSystem = new FakeFileSystem();
        var cleaner = new WinSxSCleaner();
        await cleaner.ScanAsync(TestContext.Create(fileSystem, environment, runner));
        await cleaner.CleanAsync(TestContext.Create(fileSystem, environment, runner));

        // Each run must measure its own before/after sizes.
        Assert.Equal(3, runner.Invocations.Count(i => i.Arguments.Contains("/AnalyzeComponentStore")));
    }
}

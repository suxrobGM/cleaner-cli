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
    public async Task WinSxSCleaner_does_not_analyze_while_scanning()
    {
        var environment = FakeEnvironment.Windows();
        environment.IsElevated = true;
        var runner = new FakeProcessRunner().WithAvailable("dism");
        runner.Result = new ProcessResult(0, DismReport, string.Empty);
        var context = TestContext.Create(environment: environment, processRunner: runner);
        var cleaner = new WinSxSCleaner();

        var scan = await cleaner.ScanAsync(context);

        // The report costs a full walk of the component store, so a scan must never wait on it.
        Assert.Equal(0, scan.TotalBytes);
        Assert.Empty(runner.Invocations);
        Assert.False(cleaner.SupportsSizeEstimate);
        Assert.True(cleaner.IsAvailable(context));
    }

    [Fact]
    public async Task WinSxSCleaner_measures_around_the_cleanup_to_report_freed_bytes()
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
        Assert.Equal(2, runner.Invocations.Count(i => i.Arguments.Contains("/AnalyzeComponentStore")));
        Assert.Contains(runner.Invocations, i => i.Arguments.Contains("/StartComponentCleanup"));
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task WinSxSCleaner_does_not_measure_without_elevation()
    {
        var runner = new FakeProcessRunner().WithAvailable("dism");
        runner.Result = new ProcessResult(0, DismReport, string.Empty);
        var context = TestContext.Create(environment: FakeEnvironment.Windows(), processRunner: runner);

        var result = await new WinSxSCleaner().CleanAsync(context);

        Assert.Equal(0, result.BytesFreed);
        Assert.DoesNotContain(runner.Invocations, i => i.Arguments.Contains("/AnalyzeComponentStore"));
    }

    [Fact]
    public async Task WinSxSCleaner_bounds_every_dism_call()
    {
        var environment = FakeEnvironment.Windows();
        environment.IsElevated = true;
        var runner = new FakeProcessRunner().WithAvailable("dism");
        runner.Result = new ProcessResult(0, DismReport, string.Empty);

        await new WinSxSCleaner().CleanAsync(TestContext.Create(environment: environment, processRunner: runner));

        // A busy servicing stack makes DISM block rather than fail, so nothing may wait forever.
        Assert.NotEmpty(runner.Invocations);
        Assert.All(runner.Invocations, i => Assert.NotNull(i.Timeout));
    }
}

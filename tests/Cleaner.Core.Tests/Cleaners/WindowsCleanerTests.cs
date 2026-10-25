using Cleaner.Cli.Infrastructure;
using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Os;
using Cleaner.Core.Services;
using Cleaner.Core.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cleaner.Core.Tests;

public sealed class WindowsCleanerTests
{
    [Fact]
    public async Task WindowsLogCleaner_clears_the_servicing_logs()
    {
        const string windows = @"C:\Windows";
        var fs = new FakeFileSystem()
            .AddFile($@"{windows}\Logs\CBS\CBS.log", 4_000)
            .AddFile($@"{windows}\Logs\DISM\dism.log", 1_000)
            .AddFile($@"{windows}\System32\kernel32.dll", 9_999);
        var env = new FakeEnvironment { Os = OsPlatform.Windows, WindowsDirectory = windows };

        var cleaner = new WindowsLogCleaner();
        var result = await cleaner.CleanAsync(TestContext.Create(fs, env));

        Assert.True(cleaner.RequiresElevation);
        Assert.Equal(5_000, result.BytesFreed);
        Assert.True(fs.FileExists($@"{windows}\System32\kernel32.dll"));
        Assert.False(fs.FileExists($@"{windows}\Logs\CBS\CBS.log"));
    }

    [Fact]
    public async Task ServiceProfileTempCleaner_clears_both_service_accounts()
    {
        const string windows = @"C:\Windows";
        var fs = new FakeFileSystem()
            .AddFile($@"{windows}\ServiceProfiles\LocalService\AppData\Local\Temp\a.tmp", 100)
            .AddFile($@"{windows}\ServiceProfiles\NetworkService\AppData\Local\Temp\b.tmp", 200);
        var env = new FakeEnvironment { Os = OsPlatform.Windows, WindowsDirectory = windows };

        var result = await new ServiceProfileTempCleaner().ScanAsync(TestContext.Create(fs, env));

        Assert.Equal(300, result.TotalBytes);
    }

    [Fact]
    public async Task GpuShaderCacheCleaner_clears_vendor_caches_without_elevation()
    {
        const string local = @"C:\Users\test\AppData\Local";
        var fs = new FakeFileSystem()
            .AddFile($@"{local}\D3DSCache\a.bin", 1_000)
            .AddFile($@"{local}\NVIDIA\GLCache\b.bin", 2_000);
        var env = new FakeEnvironment { Os = OsPlatform.Windows, LocalAppDataDirectory = local };

        var cleaner = new GpuShaderCacheCleaner();
        var result = await cleaner.ScanAsync(TestContext.Create(fs, env));

        Assert.False(cleaner.RequiresElevation);
        Assert.Equal(3_000, result.TotalBytes);
    }

    [Fact]
    public async Task GpuShaderCacheCleaner_includes_nvidia_nv_cache()
    {
        const string local = @"C:\Users\test\AppData\Local";
        var fs = new FakeFileSystem().AddFile($@"{local}\NVIDIA\NV_Cache\shader.bin", 3_000);
        var env = new FakeEnvironment { Os = OsPlatform.Windows, LocalAppDataDirectory = local };

        var result = await new GpuShaderCacheCleaner().ScanAsync(TestContext.Create(fs, env));

        Assert.Equal(3_000, result.TotalBytes);
    }

    [Fact]
    public async Task StoreAppCacheCleaner_clears_package_caches_but_not_local_state()
    {
        const string local = @"C:\Users\test\AppData\Local";
        var pkg = $@"{local}\Packages\Microsoft.App_8wekyb3d8bbwe";
        var fs = new FakeFileSystem()
            .AddFile($@"{pkg}\AC\INetCache\x.dat", 500)
            .AddFile($@"{pkg}\TempState\y.tmp", 300)
            .AddFile($@"{pkg}\LocalState\save.db", 9_999);
        var env = new FakeEnvironment { Os = OsPlatform.Windows, LocalAppDataDirectory = local };

        var result = await new StoreAppCacheCleaner().CleanAsync(TestContext.Create(fs, env));

        Assert.Equal(800, result.BytesFreed);
        Assert.True(fs.FileExists($@"{pkg}\LocalState\save.db"));
    }

    [Fact]
    public async Task GpuInstallerLeftoverCleaner_never_touches_driver_store()
    {
        var fs = new FakeFileSystem()
            .AddFile(@"C:\NVIDIA\DisplayDriver\560.81\setup.exe", 4_000)
            .AddFile(@"C:\AMD\Adrenalin\setup.exe", 3_000)
            .AddFile(@"C:\ProgramData\NVIDIA Corporation\Downloader\pkg.bin", 2_000)
            .AddFile(@"C:\ProgramData\NVIDIA Corporation\Installer2\core\file", 9_999)
            .AddFile(@"C:\Windows\System32\DriverStore\FileRepository\nv.inf", 9_999);
        var env = new FakeEnvironment { Os = OsPlatform.Windows, WindowsDirectory = @"C:\Windows" };

        var cleaner = new GpuInstallerLeftoverCleaner();
        var result = await cleaner.CleanAsync(TestContext.Create(fs, env));

        Assert.True(cleaner.RequiresElevation);
        Assert.Equal(9_000, result.BytesFreed);
        Assert.True(fs.FileExists(@"C:\ProgramData\NVIDIA Corporation\Installer2\core\file"));
        Assert.True(fs.FileExists(@"C:\Windows\System32\DriverStore\FileRepository\nv.inf"));
    }

    [Fact]
    public async Task WindowsOldCleaner_takes_ownership_and_deletes()
    {
        var runner = new FakeProcessRunner().WithAvailable("takeown").WithAvailable("icacls");
        var fs = new FakeFileSystem().AddFile(@"C:\Windows.old\Windows\System32\old.dll", 10_000);
        var env = new FakeEnvironment { Os = OsPlatform.Windows, WindowsDirectory = @"C:\Windows" };

        var cleaner = new WindowsOldCleaner();
        var result = await cleaner.CleanAsync(TestContext.Create(fs, env, runner));

        Assert.False(string.IsNullOrEmpty(cleaner.ConfirmationWarning));
        Assert.True(cleaner.RequiresElevation);
        Assert.Equal(10_000, result.BytesFreed);
        Assert.False(fs.DirectoryExists(@"C:\Windows.old"));
        Assert.Contains(runner.Invocations, i => i.Executable == "takeown");
        Assert.Contains(runner.Invocations, i => i.Executable == "icacls");
    }

    [Fact]
    public async Task WindowsOldCleaner_dry_run_measures_without_touching_acls()
    {
        var runner = new FakeProcessRunner();
        var fs = new FakeFileSystem().AddFile(@"C:\Windows.old\file", 8_000);
        var env = new FakeEnvironment { Os = OsPlatform.Windows, WindowsDirectory = @"C:\Windows" };

        var result = await new WindowsOldCleaner().CleanAsync(TestContext.Create(fs, env, runner, dryRun: true));

        Assert.Equal(8_000, result.BytesFreed);
        Assert.True(fs.FileExists(@"C:\Windows.old\file"));
        Assert.Empty(runner.Invocations);
    }

    [Fact]
    public async Task WinSxSCleaner_runs_dism_component_cleanup()
    {
        var runner = new FakeProcessRunner().WithAvailable("dism");
        var env = new FakeEnvironment { Os = OsPlatform.Windows, WindowsDirectory = @"C:\Windows" };

        var cleaner = new WinSxSCleaner();
        await cleaner.CleanAsync(TestContext.Create(environment: env, processRunner: runner));

        Assert.True(cleaner.RequiresElevation);
        Assert.False(cleaner.SupportsSizeEstimate);
        var invocation = Assert.Single(runner.Invocations);
        Assert.Equal("/Online /Cleanup-Image /StartComponentCleanup", string.Join(' ', invocation.Arguments));
    }

    [Fact]
    public async Task ScoopCleaner_clears_the_cache_then_lets_scoop_remove_old_versions()
    {
        var scoop = RegisteredCleaner("scoop");
        var runner = new FakeProcessRunner().WithAvailable("scoop");
        var fs = new FakeFileSystem().AddFile(@"C:\Users\test\scoop\cache\git#2.45.0#x.7z", 1_000);

        await scoop.CleanAsync(TestContext.Create(fs, FakeEnvironment.Windows(), runner));

        var commands = runner.Invocations.Select(i => string.Join(' ', i.Arguments)).ToList();
        Assert.Equal(["cache rm *", "cleanup *"], commands);
        Assert.Contains("scoop reset", scoop.ConfirmationWarning);
    }

    [Theory]
    [InlineData("SCOOP_CACHE", @"D:\scoop-cache", @"D:\scoop-cache")]
    [InlineData("SCOOP", @"D:\scoop", @"D:\scoop\cache")]
    public async Task ScoopCleaner_honors_relocated_scoop_directories(string variable, string value, string cache)
    {
        var fs = new FakeFileSystem()
            .AddFile($@"{cache}\git#2.45.0#x.7z", 1_000)
            .AddFile(@"D:\scoop\apps\git\2.44.0\git.exe", 9_999);
        var env = FakeEnvironment.Windows().SetVariable(variable, value);

        // Without scoop on PATH only the cache is deleted directly; old versions are never touched.
        var result = await RegisteredCleaner("scoop").CleanAsync(TestContext.Create(fs, env));

        Assert.Equal(1_000, result.BytesFreed);
        Assert.True(fs.FileExists(@"D:\scoop\apps\git\2.44.0\git.exe"));
    }

    private static ICleaner RegisteredCleaner(string id)
    {
        var services = new ServiceCollection();
        services.AddCleaner();
        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<ICleanerRegistry>().All.Single(c => c.Id == id);
    }
}

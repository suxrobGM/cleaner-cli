using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.DevTools;
using Cleaner.Core.Cleaners.Os;
using Cleaner.Core.Services;
using Cleaner.Core.Tests.Fakes;
using Xunit;

namespace Cleaner.Core.Tests;

/// <summary>Cleaners that need admin and carry their own confirmation.</summary>
public sealed class RiskyCleanerTests
{
    [Fact]
    public async Task DockerVhdxCleaner_shuts_wsl_down_then_compacts_each_disk()
    {
        var fs = Disks();
        var runner = new FakeProcessRunner().WithAvailable("wsl", "diskpart");
        // diskpart shrinking the file is what the cleaner measures, so model that -- and only that,
        // otherwise the shutdown call would shrink it before the "before" size is taken.
        runner.OnRun = () =>
        {
            if (runner.Invocations[^1].Executable == "diskpart")
            {
                fs.AddFile(DataDisk, 20_000);
            }
        };

        var cleaner = new DockerVhdxCleaner();
        var result = await cleaner.CleanAsync(TestContext.Create(fs, FakeEnvironment.Windows(), runner));

        Assert.True(cleaner.RequiresElevation);
        Assert.Equal(["--shutdown"], runner.Invocations[0].Arguments);
        Assert.Equal("wsl", runner.Invocations[0].Executable);
        Assert.Contains(runner.Invocations, i => i.Executable == "diskpart");
        Assert.True(result.BytesFreed > 0);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task DockerVhdxCleaner_leaves_disks_alone_when_wsl_will_not_shut_down()
    {
        var fs = Disks();
        var runner = new FakeProcessRunner().WithAvailable("wsl", "diskpart");
        runner.Result = new ProcessResult(1, string.Empty, "denied");

        var result = await new DockerVhdxCleaner().CleanAsync(TestContext.Create(fs, FakeEnvironment.Windows(), runner));

        // Compacting an attached disk corrupts it, so a failed shutdown must stop the run.
        Assert.DoesNotContain(runner.Invocations, i => i.Executable == "diskpart");
        Assert.Equal(0, result.BytesFreed);
        Assert.Single(result.Errors);
    }

    [Fact]
    public async Task DockerVhdxCleaner_reports_nothing_up_front_and_deletes_nothing_on_dry_run()
    {
        var fs = Disks();
        var runner = new FakeProcessRunner().WithAvailable("wsl", "diskpart");
        var context = new CleanupContext
        {
            FileSystem = fs,
            Environment = FakeEnvironment.Windows(),
            ProcessRunner = runner,
            DryRun = true,
        };

        var scan = await new DockerVhdxCleaner().ScanAsync(context);
        var result = await new DockerVhdxCleaner().CleanAsync(context);

        Assert.Equal(0, scan.TotalBytes);
        Assert.Empty(runner.Invocations);
        Assert.True(fs.FileExists(DataDisk));
        Assert.Equal(0, result.BytesFreed);
    }

    [Fact]
    public async Task NativeImageCacheCleaner_clears_native_images_but_never_the_gac()
    {
        var fs = new FakeFileSystem()
            .AddFile(@"C:\Windows\assembly\NativeImages_v4.0.30319_64\System\x.ni.dll", 6_000)
            .AddFile(@"C:\Windows\assembly\NativeImages_v2.0.50727_32\Old\y.ni.dll", 2_000)
            .AddFile(@"C:\Windows\assembly\GAC_MSIL\System\System.dll", 9_999)
            .AddFile(@"C:\Windows\assembly\GAC_64\Foo\foo.dll", 500);

        var cleaner = new NativeImageCacheCleaner();
        var result = await cleaner.CleanAsync(TestContext.Create(fs, FakeEnvironment.Windows()));

        Assert.True(cleaner.RequiresElevation);
        Assert.False(string.IsNullOrEmpty(cleaner.ConfirmationWarning));
        Assert.Equal(8_000, result.BytesFreed);
        Assert.True(fs.FileExists(@"C:\Windows\assembly\GAC_MSIL\System\System.dll"));
        Assert.True(fs.FileExists(@"C:\Windows\assembly\GAC_64\Foo\foo.dll"));
    }

    [Fact]
    public async Task InstallerOrphanCleaner_removes_only_unreferenced_packages()
    {
        var fs = InstallerCache();
        var runner = new FakeProcessRunner().WithAvailable("reg");
        // Registry casing differs from enumeration casing; the match must survive that.
        runner.Result = new ProcessResult(
            0,
            """
                LocalPackage    REG_SZ    C:\WINDOWS\Installer\live.msi
                LocalPackage    REG_SZ    C:\WINDOWS\Installer\patch.msp
            """,
            string.Empty);

        var result = await new WindowsInstallerOrphanCleaner().CleanAsync(TestContext.Create(fs, FakeEnvironment.Windows(), runner));

        Assert.Equal(3_000, result.BytesFreed);
        Assert.True(fs.FileExists(@"C:\Windows\Installer\live.msi"));
        Assert.True(fs.FileExists(@"C:\Windows\Installer\patch.msp"));
        Assert.True(fs.FileExists(@"C:\Windows\Installer\notes.txt"));
        Assert.False(fs.FileExists(@"C:\Windows\Installer\stale.msi"));
    }

    [Fact]
    public async Task InstallerOrphanCleaner_does_nothing_when_the_registry_gives_no_answer()
    {
        // An empty reference set would otherwise mark the whole cache as garbage.
        var fs = InstallerCache();
        var runner = new FakeProcessRunner().WithAvailable("reg");
        runner.Result = new ProcessResult(1, string.Empty, "access denied");

        var scan = await new WindowsInstallerOrphanCleaner().ScanAsync(TestContext.Create(fs, FakeEnvironment.Windows(), runner));
        var result = await new WindowsInstallerOrphanCleaner().CleanAsync(TestContext.Create(fs, FakeEnvironment.Windows(), runner));

        Assert.Equal(0, scan.TotalBytes);
        Assert.Equal(0, result.BytesFreed);
        Assert.True(fs.FileExists(@"C:\Windows\Installer\stale.msi"));
    }

    private static FakeFileSystem InstallerCache() =>
        new FakeFileSystem()
            .AddFile(@"C:\Windows\Installer\live.msi", 5_000)
            .AddFile(@"C:\Windows\Installer\patch.msp", 4_000)
            .AddFile(@"C:\Windows\Installer\stale.msi", 3_000)
            .AddFile(@"C:\Windows\Installer\notes.txt", 10);

    private const string DataDisk = @"C:\Users\test\AppData\Local\Docker\wsl\disk\docker_data.vhdx";

    private static FakeFileSystem Disks() =>
        new FakeFileSystem()
            .AddFile(DataDisk, 80_000)
            .AddFile(@"C:\Users\test\AppData\Local\Docker\wsl\disk\notes.txt", 10);
}

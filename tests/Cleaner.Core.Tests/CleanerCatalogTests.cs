using Cleaner.Core.Cleaners.Applications;
using Cleaner.Core.Cleaners.DevTools;
using Cleaner.Core.Cleaners.Os;
using Cleaner.Core.Services;
using Cleaner.Core.Tests.Fakes;
using Xunit;

namespace Cleaner.Core.Tests;

public sealed class CleanerCatalogTests
{
    [Fact]
    public async Task NuGetCleaner_finds_the_global_packages_folder()
    {
        var fs = new FakeFileSystem().AddFile("/home/test/.nuget/packages/pkg/1.0/lib.dll", 5_000);
        var env = new FakeEnvironment { HomeDirectory = "/home/test", Os = OsPlatform.Linux };

        var result = await new NuGetCleaner().ScanAsync(TestContext.Create(fs, env));

        Assert.Equal(5_000, result.TotalBytes);
    }

    [Fact]
    public async Task SteamCleaner_clears_caches_but_never_installed_games()
    {
        const string root = "/home/test/.steam/steam";
        var fs = new FakeFileSystem()
            .AddFile($"{root}/steamapps/shadercache/x.bin", 1_000)
            .AddFile($"{root}/steamapps/downloading/y.bin", 2_000)
            .AddFile($"{root}/appcache/httpcache/z.bin", 500)
            .AddFile($"{root}/steamapps/common/MyGame/game.exe", 9_999);
        var env = new FakeEnvironment { HomeDirectory = "/home/test", Os = OsPlatform.Linux };

        var result = await new SteamCleaner().CleanAsync(TestContext.Create(fs, env));

        Assert.Equal(3_500, result.BytesFreed);
        Assert.True(fs.FileExists($"{root}/steamapps/common/MyGame/game.exe")); // game untouched
        Assert.False(fs.FileExists($"{root}/steamapps/shadercache/x.bin")); // cache contents cleared
    }

    [Fact]
    public void Windows_only_cleaner_is_not_applicable_on_linux()
    {
        var env = new FakeEnvironment { Os = OsPlatform.Linux };
        var context = TestContext.Create(new FakeFileSystem(), env);

        Assert.False(new WindowsUpdateCacheCleaner().IsApplicable(context));
        Assert.True(new WindowsUpdateCacheCleaner().RequiresElevation);
    }

    [Fact]
    public async Task WindowsLogCleaner_clears_the_servicing_logs()
    {
        const string windows = @"C:\Windows";
        var fs = new FakeFileSystem()
            .AddFile($@"{windows}\Logs\CBS\CBS.log", 4_000)
            .AddFile($@"{windows}\Logs\DISM\dism.log", 1_000)
            .AddFile($@"{windows}\System32\kernel32.dll", 9_999); // not a target
        var env = new FakeEnvironment { Os = OsPlatform.Windows, WindowsDirectory = windows };

        var cleaner = new WindowsLogCleaner();
        var result = await cleaner.CleanAsync(TestContext.Create(fs, env));

        Assert.True(cleaner.RequiresElevation);
        Assert.Equal(5_000, result.BytesFreed);
        Assert.True(fs.FileExists($@"{windows}\System32\kernel32.dll")); // system files untouched
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
    public async Task StoreAppCacheCleaner_clears_package_caches_but_not_local_state()
    {
        const string local = @"C:\Users\test\AppData\Local";
        var pkg = $@"{local}\Packages\Microsoft.App_8wekyb3d8bbwe";
        var fs = new FakeFileSystem()
            .AddFile($@"{pkg}\AC\INetCache\x.dat", 500)
            .AddFile($@"{pkg}\TempState\y.tmp", 300)
            .AddFile($@"{pkg}\LocalState\save.db", 9_999); // real data — must survive
        var env = new FakeEnvironment { Os = OsPlatform.Windows, LocalAppDataDirectory = local };

        var result = await new StoreAppCacheCleaner().CleanAsync(TestContext.Create(fs, env));

        Assert.Equal(800, result.BytesFreed);
        Assert.True(fs.FileExists($@"{pkg}\LocalState\save.db"));
    }

    [Fact]
    public async Task ElectronAppCacheCleaner_clears_known_app_caches_but_not_config()
    {
        const string roaming = @"C:\Users\test\AppData\Roaming";
        var fs = new FakeFileSystem()
            .AddFile($@"{roaming}\discord\Cache\a.bin", 1_000)
            .AddFile($@"{roaming}\Slack\GPUCache\b.bin", 500)
            .AddFile($@"{roaming}\discord\settings.json", 42); // config — must survive
        var env = new FakeEnvironment { Os = OsPlatform.Windows, AppDataDirectory = roaming };

        var result = await new ElectronAppCacheCleaner().CleanAsync(TestContext.Create(fs, env));

        Assert.Equal(1_500, result.BytesFreed);
        Assert.True(fs.FileExists($@"{roaming}\discord\settings.json"));
    }

    [Fact]
    public async Task BuildArtifactCleaner_collects_matches_without_descending()
    {
        var fs = new FakeFileSystem()
            .AddFile("/work/src/bin/app.dll", 100)
            .AddFile("/work/src/obj/tmp.o", 50)
            .AddFile("/work/node_modules/.bin/x", 10)
            .AddFile("/work/node_modules/pkg/index.js", 200)
            .AddFile("/work/keep.txt", 1);

        var context = TestContext.Create(fs, workingDirectory: "/work");
        var result = await new BuildArtifactCleaner().ScanAsync(context);

        // bin, obj, node_modules are matched; keep.txt is not.
        var paths = result.Targets.Select(t => t.Path).ToList();
        Assert.Contains(paths, p => p.EndsWith("bin"));
        Assert.Contains(paths, p => p.EndsWith("obj"));
        Assert.Contains(paths, p => p.EndsWith("node_modules"));
    }
}

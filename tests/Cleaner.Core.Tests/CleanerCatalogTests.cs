using Cleaner.Core.Abstractions;
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

    [Fact]
    public async Task BuildArtifactCleaner_sweeps_every_scan_root()
    {
        var fs = new FakeFileSystem()
            .AddFile("/r1/proj/node_modules/pkg/index.js", 100)
            .AddFile("/r2/app/dist/bundle.js", 200)
            .AddFile("/r2/app/keep.txt", 1);
        var context = new CleanupContext
        {
            FileSystem = fs,
            Environment = new FakeEnvironment(),
            ProcessRunner = new FakeProcessRunner(),
            ScanRoots = ["/r1", "/r2"],
        };

        var result = await new BuildArtifactCleaner().ScanAsync(context);

        Assert.Equal(300, result.TotalBytes);
        var paths = result.Targets.Select(t => t.Path).ToList();
        Assert.Contains(paths, p => p.EndsWith("node_modules"));
        Assert.Contains(paths, p => p.EndsWith("dist"));
    }

    [Fact]
    public async Task MlCacheCleaner_clears_huggingface_and_torch_under_dot_cache()
    {
        var fs = new FakeFileSystem()
            .AddFile("/home/test/.cache/huggingface/hub/model.bin", 8_000)
            .AddFile("/home/test/.cache/torch/hub/weights.pt", 2_000)
            .AddFile("/home/test/.cache/keep/other.txt", 99); // unrelated cache — must survive
        var env = new FakeEnvironment { HomeDirectory = "/home/test", Os = OsPlatform.Linux };

        var result = await new MlCacheCleaner().CleanAsync(TestContext.Create(fs, env));

        Assert.Equal(10_000, result.BytesFreed);
        Assert.True(fs.FileExists("/home/test/.cache/keep/other.txt"));
    }

    [Fact]
    public async Task MlCacheCleaner_honors_HF_HOME_override()
    {
        var fs = new FakeFileSystem().AddFile("/data/hf/hub/model.bin", 5_000);
        var env = new FakeEnvironment { HomeDirectory = "/home/test", Os = OsPlatform.Linux }
            .SetVariable("HF_HOME", "/data/hf");

        var result = await new MlCacheCleaner().ScanAsync(TestContext.Create(fs, env));

        Assert.Equal(5_000, result.TotalBytes);
    }

    [Fact]
    public async Task VcpkgCleaner_clears_downloads_and_archives_on_windows()
    {
        const string local = @"C:\Users\test\AppData\Local";
        var fs = new FakeFileSystem()
            .AddFile($@"{local}\vcpkg\downloads\tool.zip", 1_000)
            .AddFile($@"{local}\vcpkg\archives\pkg.zip", 2_000)
            .AddFile($@"{local}\vcpkg\installed\lib.a", 9_999); // built packages — must survive
        var env = new FakeEnvironment { Os = OsPlatform.Windows, LocalAppDataDirectory = local };

        var result = await new VcpkgCleaner().CleanAsync(TestContext.Create(fs, env));

        Assert.Equal(3_000, result.BytesFreed);
        Assert.True(fs.FileExists($@"{local}\vcpkg\installed\lib.a"));
    }

    [Fact]
    public async Task SpotifyCleaner_clears_caches_but_not_user_data()
    {
        const string local = @"C:\Users\test\AppData\Local";
        var fs = new FakeFileSystem()
            .AddFile($@"{local}\Spotify\Storage\a.file", 4_000)
            .AddFile($@"{local}\Spotify\Data\b.file", 2_000)
            .AddFile($@"{local}\Spotify\Users\prefs", 50); // settings — must survive
        var env = new FakeEnvironment { Os = OsPlatform.Windows, LocalAppDataDirectory = local };

        var result = await new SpotifyCleaner().CleanAsync(TestContext.Create(fs, env));

        Assert.Equal(6_000, result.BytesFreed);
        Assert.True(fs.FileExists($@"{local}\Spotify\Users\prefs"));
    }

    [Fact]
    public async Task KonanCleaner_clears_cache_dependencies_and_prebuilt_distributions()
    {
        var fs = new FakeFileSystem()
            .AddFile("/home/test/.konan/cache/x.bin", 1_000)
            .AddFile("/home/test/.konan/dependencies/llvm/y.bin", 2_000)
            .AddFile("/home/test/.konan/kotlin-native-prebuilt-linux-x86_64-2.0.0/bin/konanc", 5_000);
        var env = new FakeEnvironment { HomeDirectory = "/home/test", Os = OsPlatform.Linux };

        var result = await new KonanCleaner().ScanAsync(TestContext.Create(fs, env));

        Assert.Equal(8_000, result.TotalBytes); // cache + dependencies + downloaded compiler
    }

    [Fact]
    public async Task AzureFunctionsToolsCleaner_clears_release_feeds()
    {
        const string local = @"C:\Users\test\AppData\Local";
        var fs = new FakeFileSystem().AddFile($@"{local}\AzureFunctionsTools\Releases\4.0\func.exe", 7_000);
        var env = new FakeEnvironment { Os = OsPlatform.Windows, LocalAppDataDirectory = local };

        var result = await new AzureFunctionsToolsCleaner().ScanAsync(TestContext.Create(fs, env));

        Assert.Equal(7_000, result.TotalBytes);
    }

    [Fact]
    public async Task DotslashCleaner_clears_windows_cache()
    {
        const string local = @"C:\Users\test\AppData\Local";
        var fs = new FakeFileSystem().AddFile($@"{local}\dotslash\bin\x", 1_500);
        var env = new FakeEnvironment { Os = OsPlatform.Windows, LocalAppDataDirectory = local };

        var result = await new DotslashCleaner().ScanAsync(TestContext.Create(fs, env));

        Assert.Equal(1_500, result.TotalBytes);
    }

    [Fact]
    public async Task ElectronAppCacheCleaner_now_clears_claude()
    {
        const string roaming = @"C:\Users\test\AppData\Roaming";
        var fs = new FakeFileSystem()
            .AddFile($@"{roaming}\Claude\Cache\a.bin", 12_000)
            .AddFile($@"{roaming}\Claude\config.json", 42); // config — must survive
        var env = new FakeEnvironment { Os = OsPlatform.Windows, AppDataDirectory = roaming };

        var result = await new ElectronAppCacheCleaner().CleanAsync(TestContext.Create(fs, env));

        Assert.Equal(12_000, result.BytesFreed);
        Assert.True(fs.FileExists($@"{roaming}\Claude\config.json"));
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
    public async Task BrowserAutomationCleaner_covers_dot_cache_puppeteer_on_windows()
    {
        const string local = @"C:\Users\test\AppData\Local";
        var fs = new FakeFileSystem().AddFile(@"C:\Users\test\.cache\puppeteer\chrome\x", 2_500);
        var env = new FakeEnvironment
        {
            Os = OsPlatform.Windows,
            HomeDirectory = @"C:\Users\test",
            LocalAppDataDirectory = local,
        };

        var result = await new BrowserAutomationCleaner().ScanAsync(TestContext.Create(fs, env));

        Assert.Equal(2_500, result.TotalBytes);
    }

    [Fact]
    public async Task UnityCleaner_clears_project_artifacts_only_inside_unity_projects()
    {
        var fs = new FakeFileSystem()
            // A real Unity project: has Assets + ProjectSettings.
            .AddFile("/projects/GameA/Assets/Scene.unity", 10)
            .AddFile("/projects/GameA/ProjectSettings/ProjectVersion.txt", 10)
            .AddFile("/projects/GameA/Library/artifacts/a.bin", 5_000)
            .AddFile("/projects/GameA/Temp/b.tmp", 200)
            .AddFile("/projects/GameA/Logs/c.log", 50)
            // Not a Unity project (no ProjectSettings) but has a Library — must survive.
            .AddFile("/projects/NotUnity/Library/big.bin", 9_999);
        var env = new FakeEnvironment { HomeDirectory = "/home/test", Os = OsPlatform.Linux };
        var context = new CleanupContext
        {
            FileSystem = fs,
            Environment = env,
            ProcessRunner = new FakeProcessRunner(),
            ScanRoots = ["/projects"],
        };

        var result = await new UnityCleaner().CleanAsync(context);

        Assert.Equal(5_250, result.BytesFreed); // Library + Temp + Logs of GameA
        Assert.True(fs.FileExists("/projects/NotUnity/Library/big.bin"));
        Assert.True(fs.FileExists("/projects/GameA/Assets/Scene.unity")); // assets untouched
    }

    [Fact]
    public async Task UnityCleaner_clears_global_editor_cache_on_windows()
    {
        const string local = @"C:\Users\test\AppData\Local";
        var fs = new FakeFileSystem().AddFile($@"{local}\Unity\cache\GiCache\x.bin", 4_000);
        var env = new FakeEnvironment { Os = OsPlatform.Windows, LocalAppDataDirectory = local, HomeDirectory = @"C:\Users\test" };
        var context = new CleanupContext
        {
            FileSystem = fs,
            Environment = env,
            ProcessRunner = new FakeProcessRunner(),
            ScanRoots = [@"C:\does-not-exist"],
        };

        var result = await new UnityCleaner().ScanAsync(context);

        Assert.Equal(4_000, result.TotalBytes);
    }

    [Fact]
    public async Task DockerCleaner_safe_run_prunes_system_and_build_cache()
    {
        var runner = new FakeProcessRunner().WithAvailable("docker");
        var context = new CleanupContext
        {
            FileSystem = new FakeFileSystem(),
            Environment = new FakeEnvironment(),
            ProcessRunner = runner,
            Force = false,
        };

        await new DockerCleaner().CleanAsync(context);

        Assert.Equal(2, runner.Invocations.Count);
        Assert.Equal(["system", "prune", "--force"], runner.Invocations[0].Arguments);
        Assert.Equal(["builder", "prune", "--all", "--force"], runner.Invocations[1].Arguments);
    }

    [Fact]
    public async Task DockerCleaner_force_run_also_removes_images_and_volumes()
    {
        var runner = new FakeProcessRunner().WithAvailable("docker");
        var context = new CleanupContext
        {
            FileSystem = new FakeFileSystem(),
            Environment = new FakeEnvironment(),
            ProcessRunner = runner,
            Force = true,
        };

        await new DockerCleaner().CleanAsync(context);

        Assert.Equal(2, runner.Invocations.Count);
        Assert.Equal(["system", "prune", "-a", "--volumes", "--force"], runner.Invocations[0].Arguments);
        Assert.Equal(["builder", "prune", "--all", "--force"], runner.Invocations[1].Arguments);
    }
}

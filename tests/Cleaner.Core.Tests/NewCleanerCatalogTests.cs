using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Applications;
using Cleaner.Core.Cleaners.DevTools;
using Cleaner.Core.Cleaners.Os;
using Cleaner.Core.Services;
using Cleaner.Core.Tests.Fakes;
using Xunit;

namespace Cleaner.Core.Tests;

/// <summary>
/// "Cache removed, user data survives" checks for the newer cleaners — each test plants both a
/// legitimate target and adjacent real data, then asserts only the cache went away.
/// </summary>
public sealed class NewCleanerCatalogTests
{
    [Fact]
    public async Task ConanCleaner_clears_package_cache_and_honors_CONAN_HOME()
    {
        var fs = new FakeFileSystem()
            .AddFile("/mnt/conan/p/zlib1234/p/lib/zlib.a", 4_000)
            .AddFile("/mnt/conan/profiles/default", 10); // config — must survive
        var env = new FakeEnvironment { Os = OsPlatform.Linux }.SetVariable("CONAN_HOME", "/mnt/conan");

        var result = await new ConanCleaner().CleanAsync(TestContext.Create(fs, env));

        Assert.Equal(4_000, result.BytesFreed);
        Assert.True(fs.FileExists("/mnt/conan/profiles/default"));
    }

    [Fact]
    public async Task ConanCleaner_runs_cache_clean_and_only_removes_packages_with_force()
    {
        var runner = new FakeProcessRunner().WithAvailable("conan");
        var fs = new FakeFileSystem().AddFile("/home/test/.conan2/p/pkg/file", 100);
        var env = new FakeEnvironment { HomeDirectory = "/home/test", Os = OsPlatform.Linux };

        await new ConanCleaner().CleanAsync(TestContext.Create(fs, env, runner));

        var commands = runner.Invocations.Select(i => string.Join(' ', i.Arguments)).ToList();
        Assert.Contains("cache clean *", commands);
        Assert.DoesNotContain(commands, c => c.StartsWith("remove"));
    }

    [Fact]
    public async Task JuliaCleaner_clears_compiled_but_keeps_packages_and_artifacts()
    {
        var fs = new FakeFileSystem()
            .AddFile("/home/test/.julia/compiled/v1.10/Foo/foo.ji", 2_000)
            .AddFile("/home/test/.julia/logs/repl_history.jl", 500)
            .AddFile("/home/test/.julia/packages/Foo/src/Foo.jl", 9_999)
            .AddFile("/home/test/.julia/artifacts/abc/data.bin", 9_999);
        var env = new FakeEnvironment { HomeDirectory = "/home/test", Os = OsPlatform.Linux };

        var result = await new JuliaCleaner().CleanAsync(TestContext.Create(fs, env));

        Assert.Equal(2_500, result.BytesFreed);
        Assert.True(fs.FileExists("/home/test/.julia/packages/Foo/src/Foo.jl"));
        Assert.True(fs.FileExists("/home/test/.julia/artifacts/abc/data.bin"));
    }

    [Fact]
    public async Task VagrantCleaner_keeps_boxes()
    {
        var fs = new FakeFileSystem()
            .AddFile("/home/test/.vagrant.d/tmp/partial-download", 3_000)
            .AddFile("/home/test/.vagrant.d/boxes/ubuntu/box.img", 9_999);
        var env = new FakeEnvironment { HomeDirectory = "/home/test", Os = OsPlatform.Linux };

        var result = await new VagrantCleaner().CleanAsync(TestContext.Create(fs, env));

        Assert.Equal(3_000, result.BytesFreed);
        Assert.True(fs.FileExists("/home/test/.vagrant.d/boxes/ubuntu/box.img"));
    }

    [Fact]
    public async Task Version_manager_cleaners_keep_installed_tools()
    {
        var fs = new FakeFileSystem()
            .AddFile("/home/test/.asdf/downloads/node.tar.gz", 1_000)
            .AddFile("/home/test/.asdf/installs/nodejs/20/bin/node", 9_999)
            .AddFile("/home/test/.sdkman/archives/java.zip", 2_000)
            .AddFile("/home/test/.sdkman/candidates/java/21/bin/java", 9_999)
            .AddFile("/home/test/.local/pipx/.cache/x", 300)
            .AddFile("/home/test/.local/pipx/venvs/tool/bin/tool", 9_999);
        var env = new FakeEnvironment { HomeDirectory = "/home/test", Os = OsPlatform.Linux };
        var context = TestContext.Create(fs, env);

        await new AsdfCleaner().CleanAsync(context);
        await new SdkmanCleaner().CleanAsync(context);
        await new PipxCleaner().CleanAsync(context);

        Assert.True(fs.FileExists("/home/test/.asdf/installs/nodejs/20/bin/node"));
        Assert.True(fs.FileExists("/home/test/.sdkman/candidates/java/21/bin/java"));
        Assert.True(fs.FileExists("/home/test/.local/pipx/venvs/tool/bin/tool"));
        Assert.False(fs.FileExists("/home/test/.asdf/downloads/node.tar.gz"));
        Assert.False(fs.FileExists("/home/test/.sdkman/archives/java.zip"));
        Assert.False(fs.FileExists("/home/test/.local/pipx/.cache/x"));
    }

    [Fact]
    public async Task TelegramCleaner_clears_media_cache_but_keeps_account_state()
    {
        const string tdata = @"C:\Users\test\AppData\Roaming\Telegram Desktop\tdata";
        var fs = new FakeFileSystem()
            .AddFile($@"{tdata}\user_data\cache\0\a.jpg", 5_000)
            .AddFile($@"{tdata}\user_data\media_cache\1\b.mp4", 7_000)
            .AddFile($@"{tdata}\key_datas", 9_999) // session keys — must survive
            .AddFile($@"{tdata}\D877F783D5D3EF8C\maps", 9_999); // account data — must survive
        var env = new FakeEnvironment
        {
            Os = OsPlatform.Windows,
            AppDataDirectory = @"C:\Users\test\AppData\Roaming",
        };

        var result = await new TelegramCleaner().CleanAsync(TestContext.Create(fs, env));

        Assert.Equal(12_000, result.BytesFreed);
        Assert.True(fs.FileExists($@"{tdata}\key_datas"));
        Assert.True(fs.FileExists($@"{tdata}\D877F783D5D3EF8C\maps"));
    }

    [Fact]
    public async Task GpuInstallerLeftoverCleaner_never_touches_driver_store()
    {
        var fs = new FakeFileSystem()
            .AddFile(@"C:\NVIDIA\DisplayDriver\560.81\setup.exe", 4_000)
            .AddFile(@"C:\AMD\Adrenalin\setup.exe", 3_000)
            .AddFile(@"C:\ProgramData\NVIDIA Corporation\Downloader\pkg.bin", 2_000)
            .AddFile(@"C:\ProgramData\NVIDIA Corporation\Installer2\core\file", 9_999) // needed for repair
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

        Assert.True(cleaner.RequiresForce);
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
        await cleaner.CleanAsync(TestContext.Create(new FakeFileSystem(), env, runner));

        Assert.True(cleaner.RequiresElevation);
        Assert.False(cleaner.SupportsSizeEstimate);
        var invocation = Assert.Single(runner.Invocations);
        Assert.Equal("/Online /Cleanup-Image /StartComponentCleanup", string.Join(' ', invocation.Arguments));
    }

    [Fact]
    public async Task GameLauncherCleaner_keeps_installed_games()
    {
        const string local = @"C:\Users\test\AppData\Local";
        var fs = new FakeFileSystem()
            .AddFile($@"{local}\EpicGamesLauncher\Saved\webcache_4430\data_0", 2_000)
            .AddFile($@"{local}\Battle.net\BrowserCache\Cache\f_0001", 1_000)
            .AddFile(@"C:\Program Files\Epic Games\MyGame\game.exe", 9_999);
        var env = new FakeEnvironment { Os = OsPlatform.Windows, LocalAppDataDirectory = local };

        var result = await new GameLauncherCleaner().CleanAsync(TestContext.Create(fs, env));

        Assert.Equal(3_000, result.BytesFreed);
        Assert.True(fs.FileExists(@"C:\Program Files\Epic Games\MyGame\game.exe"));
    }

    [Fact]
    public async Task UnrealCleaner_only_touches_derived_data()
    {
        const string local = @"C:\Users\test\AppData\Local";
        var fs = new FakeFileSystem()
            .AddFile($@"{local}\UnrealEngine\Common\DerivedDataCache\ddc.udd", 6_000)
            .AddFile(@"C:\Projects\MyGame\MyGame.uproject", 9_999);
        var env = new FakeEnvironment { Os = OsPlatform.Windows, LocalAppDataDirectory = local };

        var result = await new UnrealCleaner().CleanAsync(TestContext.Create(fs, env));

        Assert.Equal(6_000, result.BytesFreed);
        Assert.True(fs.FileExists(@"C:\Projects\MyGame\MyGame.uproject"));
    }

    [Fact]
    public async Task DropboxCleaner_only_touches_the_internal_cache()
    {
        var fs = new FakeFileSystem()
            .AddFile("/home/test/Dropbox/.dropbox.cache/old-version/f", 4_000)
            .AddFile("/home/test/Dropbox/Documents/important.docx", 9_999);
        var env = new FakeEnvironment { HomeDirectory = "/home/test", Os = OsPlatform.Linux };

        var result = await new DropboxCleaner().CleanAsync(TestContext.Create(fs, env));

        Assert.Equal(4_000, result.BytesFreed);
        Assert.True(fs.FileExists("/home/test/Dropbox/Documents/important.docx"));
    }

    [Fact]
    public void Posix_only_cleaners_are_not_applicable_on_windows()
    {
        var context = TestContext.Create(new FakeFileSystem(), new FakeEnvironment { Os = OsPlatform.Windows });

        Assert.False(new SwiftPmCleaner().IsApplicable(context));
        Assert.False(new OpamCleaner().IsApplicable(context));
        Assert.False(new CpanmCleaner().IsApplicable(context));
        Assert.False(new NvmCleaner().IsApplicable(context));
        Assert.False(new AsdfCleaner().IsApplicable(context));
        Assert.False(new SdkmanCleaner().IsApplicable(context));
        Assert.False(new CocoaPodsCleaner().IsApplicable(context));
        Assert.False(new TexLiveCleaner().IsApplicable(context));
        Assert.False(new AnsibleCleaner().IsApplicable(context));
        Assert.False(new LimaCleaner().IsApplicable(context));
    }

    [Fact]
    public void Windows_only_cleaners_are_not_applicable_on_linux()
    {
        var context = TestContext.Create(new FakeFileSystem(), new FakeEnvironment { Os = OsPlatform.Linux });

        Assert.False(new WingetCleaner().IsApplicable(context));
        Assert.False(new GpuInstallerLeftoverCleaner().IsApplicable(context));
        Assert.False(new WinSxSCleaner().IsApplicable(context));
        Assert.False(new WindowsOldCleaner().IsApplicable(context));
        Assert.False(new OneDriveCleaner().IsApplicable(context));
    }
}

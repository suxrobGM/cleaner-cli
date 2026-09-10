using Cleaner.Core.Cleaners.Applications;
using Cleaner.Core.Cleaners.DevTools;
using Cleaner.Core.Cleaners.Os;
using Cleaner.Core.Services;
using Cleaner.Core.Tests.Fakes;
using Xunit;

namespace Cleaner.Core.Tests;

/// <summary>Cleaners added for the caches that dominate a loaded Windows dev machine.</summary>
public sealed class NewCacheCleanerTests
{
    [Fact]
    public async Task VsCppToolsCleaner_clears_the_intellisense_store_but_keeps_the_folder()
    {
        var fs = new FakeFileSystem()
            .AddFile(@"C:\Users\test\AppData\Local\Microsoft\vscode-cpptools\ipch\big.ipch", 8_000)
            .AddFile(@"C:\Users\test\AppData\Local\Microsoft\vscode-cpptools\abc123\db.db", 2_000);

        var result = await new VsCppToolsCleaner().CleanAsync(TestContext.Create(fs, Windows()));

        Assert.Equal(10_000, result.BytesFreed);
        Assert.True(fs.DirectoryExists(@"C:\Users\test\AppData\Local\Microsoft\vscode-cpptools"));
        Assert.False(fs.FileExists(@"C:\Users\test\AppData\Local\Microsoft\vscode-cpptools\ipch\big.ipch"));
    }

    [Fact]
    public async Task AndroidStudioCleaner_clears_every_version_but_keeps_plugins()
    {
        const string google = @"C:\Users\test\AppData\Local\Google";
        var fs = new FakeFileSystem()
            .AddFile($@"{google}\AndroidStudio2025.3.2\index\i.bin", 3_000)
            .AddFile($@"{google}\AndroidStudio2025.3.4\caches\c.bin", 2_000)
            .AddFile($@"{google}\AndroidStudio2025.3.4\plugins\p.jar", 9_999)
            .AddFile($@"{google}\Chrome\User Data\Default\Cache\x", 500);

        var result = await new AndroidStudioCleaner().CleanAsync(TestContext.Create(fs, Windows()));

        Assert.Equal(5_000, result.BytesFreed);
        Assert.True(fs.FileExists($@"{google}\AndroidStudio2025.3.4\plugins\p.jar"));
        Assert.True(fs.FileExists($@"{google}\Chrome\User Data\Default\Cache\x")); // not an IDE dir
    }

    [Fact]
    public async Task AmdTelemetryCleaner_deletes_the_log_files_but_not_the_config()
    {
        const string ppc = @"C:\ProgramData\AMD\PPC";
        var fs = new FakeFileSystem()
            .AddFile($@"{ppc}\sdkusage.csv", 9_000)
            .AddFile($@"{ppc}\apprecord.csv", 1_000)
            .AddFile($@"{ppc}\config.csv", 249)
            .AddFile($@"{ppc}\upload\queued.zip", 500);

        var result = await new AmdTelemetryCleaner().CleanAsync(TestContext.Create(fs, Windows()));

        Assert.Equal(10_500, result.BytesFreed);
        Assert.True(fs.FileExists($@"{ppc}\config.csv"));
        Assert.False(fs.FileExists($@"{ppc}\sdkusage.csv"));
    }

    [Fact]
    public async Task RazerCleaner_deletes_fps_history_but_keeps_settings()
    {
        const string cortex = @"C:\ProgramData\Razer\RazerCortex";
        var fs = new FakeFileSystem()
            .AddFile($@"{cortex}\CortexFPSData.db3", 7_000)
            .AddFile($@"{cortex}\Log\cortex.log", 300)
            .AddFile($@"{cortex}\AppConfig.xml", 120)
            .AddFile($@"{cortex}\Config\profile.json", 80);

        var result = await new RazerCleaner().CleanAsync(TestContext.Create(fs, Windows()));

        Assert.Equal(7_300, result.BytesFreed);
        Assert.True(fs.FileExists($@"{cortex}\AppConfig.xml"));
        Assert.True(fs.FileExists($@"{cortex}\Config\profile.json"));
    }

    [Fact]
    public async Task WinReAgentCleaner_removes_the_setup_scratch_folder()
    {
        var fs = new FakeFileSystem().AddFile(@"C:\$WinREAgent\Scratch\x.tmp", 4_000);
        var env = Windows();

        var cleaner = new WinReAgentCleaner();
        var result = await cleaner.CleanAsync(TestContext.Create(fs, env));

        Assert.True(cleaner.RequiresElevation);
        Assert.Equal(4_000, result.BytesFreed);
        Assert.False(fs.DirectoryExists(@"C:\$WinREAgent"));
    }

    [Fact]
    public async Task ClaudeDesktopCleaner_removes_vm_images_but_not_history()
    {
        const string root = @"C:\Users\test\AppData\Roaming\Claude";
        var fs = new FakeFileSystem()
            .AddFile($@"{root}\vm_bundles\claudevm.bundle\rootfs.vhdx", 9_000)
            .AddFile($@"{root}\Local Storage\leveldb\000001.log", 500)
            .AddFile($@"{root}\config.json", 100);

        var result = await new ClaudeDesktopCleaner().CleanAsync(TestContext.Create(fs, Windows()));

        Assert.Equal(9_000, result.BytesFreed);
        Assert.True(fs.FileExists($@"{root}\config.json"));
        Assert.True(fs.FileExists($@"{root}\Local Storage\leveldb\000001.log"));
    }

    [Fact]
    public async Task CodexCleaner_clears_scratch_and_rotated_logs_but_keeps_sessions_and_auth()
    {
        const string root = "/home/test/.codex";
        var fs = new FakeFileSystem()
            .AddFile($"{root}/cache/blob", 2_000)
            .AddFile($"{root}/.tmp/scratch", 3_000)
            .AddFile($"{root}/sandbox.2026-09-08.log", 400)
            .AddFile($"{root}/sessions/today.jsonl", 9_999)
            .AddFile($"{root}/auth.json", 50)
            .AddFile($"{root}/history.jsonl", 60);
        var env = new FakeEnvironment { HomeDirectory = "/home/test", Os = OsPlatform.Linux };

        var result = await new CodexCleaner().CleanAsync(TestContext.Create(fs, env));

        Assert.Equal(5_400, result.BytesFreed);
        Assert.True(fs.FileExists($"{root}/sessions/today.jsonl"));
        Assert.True(fs.FileExists($"{root}/auth.json"));
        Assert.True(fs.FileExists($"{root}/history.jsonl"));
        Assert.False(fs.FileExists($"{root}/sandbox.2026-09-08.log"));
    }

    [Fact]
    public async Task BrowserCacheCleaner_clears_on_device_models_but_keeps_profile_data()
    {
        const string chrome = @"C:\Users\test\AppData\Local\Google\Chrome\User Data";
        var fs = new FakeFileSystem()
            .AddFile($@"{chrome}\OptGuideOnDeviceModel\weights.bin", 4_000)
            .AddFile($@"{chrome}\Default\Cache\entry", 1_000)
            .AddFile($@"{chrome}\Default\Cookies", 900)
            .AddFile($@"{chrome}\Default\History", 800);

        var result = await new BrowserCacheCleaner().CleanAsync(TestContext.Create(fs, Windows()));

        Assert.Equal(5_000, result.BytesFreed);
        Assert.True(fs.FileExists($@"{chrome}\Default\Cookies"));
        Assert.True(fs.FileExists($@"{chrome}\Default\History"));
    }

    [Fact]
    public async Task GpuInstallerCleaner_clears_nvidia_app_staging_and_ngx_models()
    {
        var fs = new FakeFileSystem()
            .AddFile(@"C:\ProgramData\NVIDIA Corporation\NVIDIA app\UpdateFramework\driver.exe", 5_000)
            .AddFile(@"C:\ProgramData\NVIDIA\NGX\models\dlss.bin", 4_000)
            .AddFile(@"C:\Windows\System32\DriverStore\FileRepository\nv.inf", 9_999);

        var result = await new GpuInstallerLeftoverCleaner().CleanAsync(TestContext.Create(fs, Windows()));

        Assert.Equal(9_000, result.BytesFreed);
        Assert.True(fs.FileExists(@"C:\Windows\System32\DriverStore\FileRepository\nv.inf"));
    }

    [Fact]
    public async Task VsCodeCleaner_clears_web_storage_and_crashpad()
    {
        const string code = @"C:\Users\test\AppData\Roaming\Code";
        var fs = new FakeFileSystem()
            .AddFile($@"{code}\WebStorage\1\file", 6_000)
            .AddFile($@"{code}\Crashpad\reports\r.dmp", 1_000)
            .AddFile($@"{code}\User\settings.json", 200);

        var result = await new VsCodeCleaner().CleanAsync(TestContext.Create(fs, Windows()));

        Assert.Equal(7_000, result.BytesFreed);
        Assert.True(fs.FileExists($@"{code}\User\settings.json"));
    }

    private static FakeEnvironment Windows() => new()
    {
        Os = OsPlatform.Windows,
        HomeDirectory = @"C:\Users\test",
        LocalAppDataDirectory = @"C:\Users\test\AppData\Local",
        AppDataDirectory = @"C:\Users\test\AppData\Roaming",
        WindowsDirectory = @"C:\Windows",
    };
}

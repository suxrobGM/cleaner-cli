using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Applications;
using Cleaner.Core.Cleaners.DevTools;
using Cleaner.Core.Cleaners.Os;
using Cleaner.Core.Services;
using Cleaner.Core.Tests.Fakes;
using Xunit;

namespace Cleaner.Core.Tests;

public sealed class ApplicationCleanerTests
{
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
}

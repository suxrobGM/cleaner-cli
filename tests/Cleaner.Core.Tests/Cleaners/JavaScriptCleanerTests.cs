using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Applications;
using Cleaner.Core.Cleaners.DevTools;
using Cleaner.Core.Cleaners.Os;
using Cleaner.Core.Services;
using Cleaner.Core.Tests.Fakes;
using Xunit;

namespace Cleaner.Core.Tests;

public sealed class JavaScriptCleanerTests
{
    [Fact]
    public async Task YarnCleaner_covers_classic_and_berry_caches_on_macos()
    {
        var fs = new FakeFileSystem()
            .AddFile("/home/test/Library/Caches/Yarn/v6/pkg.zip", 1_000)
            .AddFile("/home/test/.yarn/berry/cache/pkg-2.zip", 2_000);
        var env = new FakeEnvironment
        {
            HomeDirectory = "/home/test",
            CacheDirectory = "/home/test/Library/Caches",
            Os = OsPlatform.MacOs,
        };

        var result = await new YarnCleaner().ScanAsync(TestContext.Create(fs, env));

        Assert.Equal(3_000, result.TotalBytes);
    }

    [Fact]
    public async Task BunCleaner_prefers_bun_pm_cache_rm()
    {
        var runner = new FakeProcessRunner().WithAvailable("bun");
        var fs = new FakeFileSystem().AddFile("/home/test/.bun/install/cache/react@18/index.js", 1_000);
        var env = new FakeEnvironment { HomeDirectory = "/home/test", Os = OsPlatform.Linux };

        await new BunCleaner().CleanAsync(TestContext.Create(fs, env, runner));

        Assert.Equal(("bun", "pm cache rm"), (runner.Invocations[0].Executable, string.Join(' ', runner.Invocations[0].Arguments)));
        Assert.True(fs.FileExists("/home/test/.bun/install/cache/react@18/index.js"));
    }

    [Fact]
    public async Task ExpoCleaner_clears_the_project_cache_but_keeps_the_global_login()
    {
        var fs = new FakeFileSystem()
            .AddFile("/work/.expo/web/cache/a.bin", 1_000)
            .AddFile("/home/test/.expo/state.json", 9_999);
        var env = new FakeEnvironment { HomeDirectory = "/home/test", Os = OsPlatform.Linux };

        var result = await new ExpoCleaner().CleanAsync(TestContext.Create(fs, env, workingDirectory: "/work"));

        Assert.Equal(1_000, result.BytesFreed);
        Assert.True(fs.FileExists("/home/test/.expo/state.json"));
    }
}

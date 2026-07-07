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
}

using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Applications;
using Cleaner.Core.Cleaners.DevTools;
using Cleaner.Core.Cleaners.Os;
using Cleaner.Core.Services;
using Cleaner.Core.Tests.Fakes;
using Xunit;

namespace Cleaner.Core.Tests;

public sealed class JvmCleanerTests
{
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
}

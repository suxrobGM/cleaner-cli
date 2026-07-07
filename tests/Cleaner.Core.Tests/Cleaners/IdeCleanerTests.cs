using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Applications;
using Cleaner.Core.Cleaners.DevTools;
using Cleaner.Core.Cleaners.Os;
using Cleaner.Core.Services;
using Cleaner.Core.Tests.Fakes;
using Xunit;

namespace Cleaner.Core.Tests;

public sealed class IdeCleanerTests
{
    [Fact]
    public async Task JetBrainsCleaner_keeps_toolbox_and_local_history_on_windows()
    {
        const string local = @"C:\Users\test\AppData\Local";
        var fs = new FakeFileSystem()
            .AddFile($@"{local}\JetBrains\Toolbox\apps\rider\rider64.exe", 9_999)
            .AddFile($@"{local}\JetBrains\Rider2024.3\caches\index.bin", 1_000)
            .AddFile($@"{local}\JetBrains\Rider2024.3\log\idea.log", 500)
            .AddFile($@"{local}\JetBrains\Rider2024.3\LocalHistory\history.db", 7_777);
        var env = new FakeEnvironment { Os = OsPlatform.Windows, LocalAppDataDirectory = local };

        var result = await new JetBrainsCleaner().CleanAsync(TestContext.Create(fs, env));

        Assert.Equal(1_500, result.BytesFreed);
        Assert.True(fs.FileExists($@"{local}\JetBrains\Toolbox\apps\rider\rider64.exe")); // installed IDE untouched
        Assert.True(fs.FileExists($@"{local}\JetBrains\Rider2024.3\LocalHistory\history.db")); // user data untouched
        Assert.False(fs.FileExists($@"{local}\JetBrains\Rider2024.3\caches\index.bin"));
    }

    [Fact]
    public async Task JetBrainsCleaner_clears_the_cache_root_on_linux()
    {
        var fs = new FakeFileSystem().AddFile("/home/test/.cache/JetBrains/Rider2024.3/caches/a.bin", 2_000);
        var env = new FakeEnvironment { HomeDirectory = "/home/test", Os = OsPlatform.Linux };

        var result = await new JetBrainsCleaner().ScanAsync(TestContext.Create(fs, env));

        Assert.Equal(2_000, result.TotalBytes);
    }
}

using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Applications;
using Cleaner.Core.Cleaners.DevTools;
using Cleaner.Core.Cleaners.Os;
using Cleaner.Core.Services;
using Cleaner.Core.Tests.Fakes;
using Xunit;

namespace Cleaner.Core.Tests;

public sealed class ToolingDownloadCleanerTests
{
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
}

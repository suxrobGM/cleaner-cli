using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Applications;
using Cleaner.Core.Cleaners.DevTools;
using Cleaner.Core.Cleaners.Os;
using Cleaner.Core.Services;
using Cleaner.Core.Tests.Fakes;
using Xunit;

namespace Cleaner.Core.Tests;

public sealed class DotnetCleanerTests
{
    [Fact]
    public async Task NuGetCleaner_finds_the_global_packages_folder()
    {
        var fs = new FakeFileSystem().AddFile("/home/test/.nuget/packages/pkg/1.0/lib.dll", 5_000);
        var env = new FakeEnvironment { HomeDirectory = "/home/test", Os = OsPlatform.Linux };

        var result = await new NuGetCleaner().ScanAsync(TestContext.Create(fs, env));

        Assert.Equal(5_000, result.TotalBytes);
    }
}

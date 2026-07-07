using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Applications;
using Cleaner.Core.Cleaners.DevTools;
using Cleaner.Core.Cleaners.Os;
using Cleaner.Core.Services;
using Cleaner.Core.Tests.Fakes;
using Xunit;

namespace Cleaner.Core.Tests;

public sealed class VersionManagerCleanerTests
{
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
}

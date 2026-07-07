using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Applications;
using Cleaner.Core.Cleaners.DevTools;
using Cleaner.Core.Cleaners.Os;
using Cleaner.Core.Services;
using Cleaner.Core.Tests.Fakes;
using Xunit;

namespace Cleaner.Core.Tests;

public sealed class GameDevCleanerTests
{
    [Fact]
    public async Task UnityCleaner_clears_project_artifacts_only_inside_unity_projects()
    {
        var fs = new FakeFileSystem()
            // A real Unity project: has Assets + ProjectSettings.
            .AddFile("/projects/GameA/Assets/Scene.unity", 10)
            .AddFile("/projects/GameA/ProjectSettings/ProjectVersion.txt", 10)
            .AddFile("/projects/GameA/Library/artifacts/a.bin", 5_000)
            .AddFile("/projects/GameA/Temp/b.tmp", 200)
            .AddFile("/projects/GameA/Logs/c.log", 50)
            // Not a Unity project (no ProjectSettings) but has a Library — must survive.
            .AddFile("/projects/NotUnity/Library/big.bin", 9_999);
        var env = new FakeEnvironment { HomeDirectory = "/home/test", Os = OsPlatform.Linux };
        var context = new CleanupContext
        {
            FileSystem = fs,
            Environment = env,
            ProcessRunner = new FakeProcessRunner(),
            ScanRoots = ["/projects"],
        };

        var result = await new UnityCleaner().CleanAsync(context);

        Assert.Equal(5_250, result.BytesFreed); // Library + Temp + Logs of GameA
        Assert.True(fs.FileExists("/projects/NotUnity/Library/big.bin"));
        Assert.True(fs.FileExists("/projects/GameA/Assets/Scene.unity")); // assets untouched
    }

    [Fact]
    public async Task UnityCleaner_clears_global_editor_cache_on_windows()
    {
        const string local = @"C:\Users\test\AppData\Local";
        var fs = new FakeFileSystem().AddFile($@"{local}\Unity\cache\GiCache\x.bin", 4_000);
        var env = new FakeEnvironment { Os = OsPlatform.Windows, LocalAppDataDirectory = local, HomeDirectory = @"C:\Users\test" };
        var context = new CleanupContext
        {
            FileSystem = fs,
            Environment = env,
            ProcessRunner = new FakeProcessRunner(),
            ScanRoots = [@"C:\does-not-exist"],
        };

        var result = await new UnityCleaner().ScanAsync(context);

        Assert.Equal(4_000, result.TotalBytes);
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
}

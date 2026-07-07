using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Applications;
using Cleaner.Core.Cleaners.DevTools;
using Cleaner.Core.Cleaners.Os;
using Cleaner.Core.Services;
using Cleaner.Core.Tests.Fakes;
using Xunit;

namespace Cleaner.Core.Tests;

public sealed class LanguageCleanerTests
{
    [Fact]
    public async Task VcpkgCleaner_clears_downloads_and_archives_on_windows()
    {
        const string local = @"C:\Users\test\AppData\Local";
        var fs = new FakeFileSystem()
            .AddFile($@"{local}\vcpkg\downloads\tool.zip", 1_000)
            .AddFile($@"{local}\vcpkg\archives\pkg.zip", 2_000)
            .AddFile($@"{local}\vcpkg\installed\lib.a", 9_999); // built packages — must survive
        var env = new FakeEnvironment { Os = OsPlatform.Windows, LocalAppDataDirectory = local };

        var result = await new VcpkgCleaner().CleanAsync(TestContext.Create(fs, env));

        Assert.Equal(3_000, result.BytesFreed);
        Assert.True(fs.FileExists($@"{local}\vcpkg\installed\lib.a"));
    }

    [Fact]
    public async Task ConanCleaner_clears_package_cache_and_honors_CONAN_HOME()
    {
        var fs = new FakeFileSystem()
            .AddFile("/mnt/conan/p/zlib1234/p/lib/zlib.a", 4_000)
            .AddFile("/mnt/conan/profiles/default", 10); // config — must survive
        var env = new FakeEnvironment { Os = OsPlatform.Linux }.SetVariable("CONAN_HOME", "/mnt/conan");

        var result = await new ConanCleaner().CleanAsync(TestContext.Create(fs, env));

        Assert.Equal(4_000, result.BytesFreed);
        Assert.True(fs.FileExists("/mnt/conan/profiles/default"));
    }

    [Fact]
    public async Task ConanCleaner_runs_cache_clean_and_only_removes_packages_with_force()
    {
        var runner = new FakeProcessRunner().WithAvailable("conan");
        var fs = new FakeFileSystem().AddFile("/home/test/.conan2/p/pkg/file", 100);
        var env = new FakeEnvironment { HomeDirectory = "/home/test", Os = OsPlatform.Linux };

        await new ConanCleaner().CleanAsync(TestContext.Create(fs, env, runner));

        var commands = runner.Invocations.Select(i => string.Join(' ', i.Arguments)).ToList();
        Assert.Contains("cache clean *", commands);
        Assert.DoesNotContain(commands, c => c.StartsWith("remove"));
    }

    [Fact]
    public async Task JuliaCleaner_clears_compiled_but_keeps_packages_and_artifacts()
    {
        var fs = new FakeFileSystem()
            .AddFile("/home/test/.julia/compiled/v1.10/Foo/foo.ji", 2_000)
            .AddFile("/home/test/.julia/logs/repl_history.jl", 500)
            .AddFile("/home/test/.julia/packages/Foo/src/Foo.jl", 9_999)
            .AddFile("/home/test/.julia/artifacts/abc/data.bin", 9_999);
        var env = new FakeEnvironment { HomeDirectory = "/home/test", Os = OsPlatform.Linux };

        var result = await new JuliaCleaner().CleanAsync(TestContext.Create(fs, env));

        Assert.Equal(2_500, result.BytesFreed);
        Assert.True(fs.FileExists("/home/test/.julia/packages/Foo/src/Foo.jl"));
        Assert.True(fs.FileExists("/home/test/.julia/artifacts/abc/data.bin"));
    }
}

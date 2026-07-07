using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Applications;
using Cleaner.Core.Cleaners.DevTools;
using Cleaner.Core.Cleaners.Os;
using Cleaner.Core.Services;
using Cleaner.Core.Tests.Fakes;
using Xunit;

namespace Cleaner.Core.Tests;

public sealed class PythonCleanerTests
{
    [Fact]
    public async Task CondaCleaner_finds_the_install_root_package_cache_and_keeps_envs()
    {
        var fs = new FakeFileSystem()
            .AddFile("/home/test/miniconda3/pkgs/numpy-1.0.tar.bz2", 6_000)
            .AddFile("/home/test/miniconda3/envs/proj/bin/python", 9_999); // installed env — must survive
        var env = new FakeEnvironment { HomeDirectory = "/home/test", Os = OsPlatform.Linux };

        var result = await new CondaCleaner().CleanAsync(TestContext.Create(fs, env));

        Assert.Equal(6_000, result.BytesFreed);
        Assert.True(fs.FileExists("/home/test/miniconda3/envs/proj/bin/python"));
    }

    [Fact]
    public async Task CondaCleaner_honors_CONDA_PKGS_DIRS()
    {
        var fs = new FakeFileSystem().AddFile("/data/conda-pkgs/pkg.conda", 2_500);
        var env = new FakeEnvironment { Os = OsPlatform.Linux }.SetVariable("CONDA_PKGS_DIRS", "/data/conda-pkgs");

        var result = await new CondaCleaner().ScanAsync(TestContext.Create(fs, env));

        Assert.Equal(2_500, result.TotalBytes);
    }
}

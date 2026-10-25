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
            .AddFile("/home/test/miniconda3/envs/proj/bin/python", 9_999);
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

    [Fact]
    public async Task CondaCleaner_without_conda_deletes_only_package_archives()
    {
        // Extracted packages may be hard-linked into environments; only conda knows which.
        const string pkgs = "/home/test/miniforge3/pkgs";
        var fs = new FakeFileSystem()
            .AddFile($"{pkgs}/numpy-1.0.conda", 1_000)
            .AddFile($"{pkgs}/zlib-1.2.tar.bz2", 2_000)
            .AddFile($"{pkgs}/numpy-1.0/lib/core.so", 9_999)
            .AddFile($"{pkgs}/urls.txt", 10)
            .AddFile($"{pkgs}/cache/repodata.json", 500);
        var env = new FakeEnvironment { HomeDirectory = "/home/test", Os = OsPlatform.Linux };

        var result = await new CondaCleaner().CleanAsync(TestContext.Create(fs, env));

        Assert.Equal(3_000, result.BytesFreed);
        Assert.False(fs.FileExists($"{pkgs}/numpy-1.0.conda"));
        Assert.False(fs.FileExists($"{pkgs}/zlib-1.2.tar.bz2"));
        Assert.True(fs.FileExists($"{pkgs}/numpy-1.0/lib/core.so"));
        Assert.True(fs.FileExists($"{pkgs}/urls.txt"));
        Assert.True(fs.FileExists($"{pkgs}/cache/repodata.json"));
    }

    [Fact]
    public async Task PoetryCleaner_keeps_virtualenvs()
    {
        const string root = "/home/test/.cache/pypoetry";
        var fs = new FakeFileSystem()
            .AddFile($"{root}/cache/repositories/PyPI/a", 1_000)
            .AddFile($"{root}/artifacts/ab/cd/pkg.whl", 2_000)
            .AddFile($"{root}/virtualenvs/proj-py3.12/bin/python", 9_999);
        var env = new FakeEnvironment { HomeDirectory = "/home/test", Os = OsPlatform.Linux };

        var result = await new PoetryCleaner().CleanAsync(TestContext.Create(fs, env));

        Assert.Equal(3_000, result.BytesFreed);
        Assert.True(fs.FileExists($"{root}/virtualenvs/proj-py3.12/bin/python"));
    }

    /// <summary>Cleaner, its executable, the expected command, and a file in its cache.</summary>
    public static TheoryData<Func<ICleaner>, string, string, string> NativeCacheCommands => new()
    {
        { () => new UvCleaner(), "uv", "cache clean", "/home/test/.cache/uv/wheels-v1/pkg.whl" },
        { () => new PreCommitCleaner(), "pre-commit", "clean", "/home/test/.cache/pre-commit/repo123/env" },
    };

    [Theory]
    [MemberData(nameof(NativeCacheCommands))]
    public async Task Python_tool_cleaners_prefer_the_tool_cache_command(
        Func<ICleaner> factory, string executable, string command, string cachedFile)
    {
        var env = new FakeEnvironment { HomeDirectory = "/home/test", Os = OsPlatform.Linux };

        // With the tool on PATH, the tool clears its own cache.
        var runner = new FakeProcessRunner().WithAvailable(executable);
        var fs = new FakeFileSystem().AddFile(cachedFile, 1_000);
        await factory().CleanAsync(TestContext.Create(fs, env, runner));

        Assert.Single(runner.Invocations);
        Assert.Equal((executable, command), (runner.Invocations[0].Executable, string.Join(' ', runner.Invocations[0].Arguments)));
        Assert.True(fs.FileExists(cachedFile));

        // Without it, the declared directory is deleted directly.
        var bare = new FakeFileSystem().AddFile(cachedFile, 1_000);
        var result = await factory().CleanAsync(TestContext.Create(bare, env));

        Assert.Equal(1_000, result.BytesFreed);
        Assert.False(bare.FileExists(cachedFile));
    }
}

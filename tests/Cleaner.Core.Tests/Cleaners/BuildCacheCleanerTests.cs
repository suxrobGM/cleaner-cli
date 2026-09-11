using Cleaner.Core.Cleaners.DevTools;
using Cleaner.Core.Tests.Fakes;
using Cleaner.Core.Utils;
using Xunit;

namespace Cleaner.Core.Tests;

public sealed class BuildCacheCleanerTests
{
    [Fact]
    public async Task BuildArtifactCleaner_sweeps_only_the_selected_folders()
    {
        var fs = new FakeFileSystem()
            .AddFile("/r/keep/node_modules/pkg/index.js", 100)
            .AddFile("/r/drop/node_modules/pkg/index.js", 200)
            .AddFile("/r/drop/dist/bundle.js", 40);
        var selected = PathComparison.CreateSet(
            ["/r/drop/node_modules", "/r/drop/dist"],
            isLinux: false);
        var context = TestContext.Create(fs, scanRoots: ["/r"], selectedPaths: selected);

        var result = await new BuildArtifactCleaner().CleanAsync(context);

        Assert.Equal(240, result.BytesFreed);
        Assert.True(fs.DirectoryExists("/r/keep/node_modules"));
        Assert.False(fs.DirectoryExists("/r/drop/node_modules"));
        Assert.False(fs.DirectoryExists("/r/drop/dist"));
    }

    [Fact]
    public async Task BuildArtifactCleaner_collects_matches_without_descending()
    {
        var fs = new FakeFileSystem()
            .AddFile("/work/src/bin/app.dll", 100)
            .AddFile("/work/src/obj/tmp.o", 50)
            .AddFile("/work/node_modules/.bin/x", 10)
            .AddFile("/work/node_modules/pkg/index.js", 200)
            .AddFile("/work/keep.txt", 1);

        var context = TestContext.Create(fs, workingDirectory: "/work");
        var result = await new BuildArtifactCleaner().ScanAsync(context);

        var paths = result.Targets.Select(t => t.Path).ToList();
        Assert.Contains(paths, p => p.EndsWith("bin"));
        Assert.Contains(paths, p => p.EndsWith("obj"));
        Assert.Contains(paths, p => p.EndsWith("node_modules"));
    }

    [Fact]
    public async Task BuildArtifactCleaner_sweeps_python_virtualenvs()
    {
        var fs = new FakeFileSystem()
            .AddFile("/work/api/.venv/lib/site-packages/torch.so", 5_000)
            .AddFile("/work/cli/venv/lib/x.py", 1_000)
            .AddFile("/work/api/main.py", 10);

        var result = await new BuildArtifactCleaner().CleanAsync(TestContext.Create(fs, workingDirectory: "/work"));

        Assert.Equal(6_000, result.BytesFreed);
        Assert.True(fs.FileExists("/work/api/main.py"));
    }

    [Fact]
    public async Task BuildArtifactCleaner_takes_build_only_beside_a_build_system()
    {
        var fs = new FakeFileSystem()
            .AddFile("/work/android/build.gradle", 10)
            .AddFile("/work/android/build/outputs/app.apk", 4_000)
            .AddFile("/work/native/CMakeLists.txt", 10)
            .AddFile("/work/native/build/libfoo.a", 2_000)
            .AddFile("/work/docs/build/index.html", 9_999);

        var result = await new BuildArtifactCleaner().CleanAsync(TestContext.Create(fs, workingDirectory: "/work"));

        Assert.Equal(6_000, result.BytesFreed);
        Assert.True(fs.FileExists("/work/docs/build/index.html"));
        Assert.True(fs.FileExists("/work/android/build.gradle"));
    }

    [Fact]
    public async Task BuildArtifactCleaner_sweeps_every_scan_root()
    {
        var fs = new FakeFileSystem()
            .AddFile("/r1/proj/node_modules/pkg/index.js", 100)
            .AddFile("/r2/app/dist/bundle.js", 200)
            .AddFile("/r2/app/keep.txt", 1);
        var context = TestContext.Create(fs, scanRoots: ["/r1", "/r2"]);

        var result = await new BuildArtifactCleaner().ScanAsync(context);

        Assert.Equal(300, result.TotalBytes);
        var paths = result.Targets.Select(t => t.Path).ToList();
        Assert.Contains(paths, p => p.EndsWith("node_modules"));
        Assert.Contains(paths, p => p.EndsWith("dist"));
    }
}

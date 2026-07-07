using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Applications;
using Cleaner.Core.Cleaners.DevTools;
using Cleaner.Core.Cleaners.Os;
using Cleaner.Core.Services;
using Cleaner.Core.Tests.Fakes;
using Xunit;

namespace Cleaner.Core.Tests;

public sealed class BuildCacheCleanerTests
{
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

        // bin, obj, node_modules are matched; keep.txt is not.
        var paths = result.Targets.Select(t => t.Path).ToList();
        Assert.Contains(paths, p => p.EndsWith("bin"));
        Assert.Contains(paths, p => p.EndsWith("obj"));
        Assert.Contains(paths, p => p.EndsWith("node_modules"));
    }

    [Fact]
    public async Task BuildArtifactCleaner_sweeps_every_scan_root()
    {
        var fs = new FakeFileSystem()
            .AddFile("/r1/proj/node_modules/pkg/index.js", 100)
            .AddFile("/r2/app/dist/bundle.js", 200)
            .AddFile("/r2/app/keep.txt", 1);
        var context = new CleanupContext
        {
            FileSystem = fs,
            Environment = new FakeEnvironment(),
            ProcessRunner = new FakeProcessRunner(),
            ScanRoots = ["/r1", "/r2"],
        };

        var result = await new BuildArtifactCleaner().ScanAsync(context);

        Assert.Equal(300, result.TotalBytes);
        var paths = result.Targets.Select(t => t.Path).ToList();
        Assert.Contains(paths, p => p.EndsWith("node_modules"));
        Assert.Contains(paths, p => p.EndsWith("dist"));
    }
}

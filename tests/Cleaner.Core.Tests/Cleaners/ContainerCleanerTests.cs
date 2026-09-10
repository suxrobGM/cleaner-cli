using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Applications;
using Cleaner.Core.Cleaners.DevTools;
using Cleaner.Core.Cleaners.Os;
using Cleaner.Core.Services;
using Cleaner.Core.Tests.Fakes;
using Xunit;

namespace Cleaner.Core.Tests;

public sealed class ContainerCleanerTests
{
    [Fact]
    public async Task DockerCleaner_prunes_images_volumes_and_build_cache()
    {
        var runner = new FakeProcessRunner().WithAvailable("docker");
        var context = new CleanupContext
        {
            FileSystem = new FakeFileSystem(),
            Environment = new FakeEnvironment(),
            ProcessRunner = runner,
        };

        await new DockerCleaner().CleanAsync(context);

        // The prune pair, bracketed by the disk-usage queries the freed total is measured with.
        var pruned = runner.Invocations.Where(i => i.Arguments.Contains("prune")).ToList();
        Assert.Equal(2, pruned.Count);
        Assert.Equal(["system", "prune", "-a", "--volumes", "--force"], pruned[0].Arguments);
        Assert.Equal(["builder", "prune", "--all", "--force"], pruned[1].Arguments);
    }

    [Fact]
    public async Task PodmanCleaner_prunes_images_and_volumes()
    {
        var runner = new FakeProcessRunner().WithAvailable("podman");
        var context = new CleanupContext
        {
            FileSystem = new FakeFileSystem(),
            Environment = new FakeEnvironment(),
            ProcessRunner = runner,
        };

        await new PodmanCleaner().CleanAsync(context);

        Assert.Single(runner.Invocations);
        Assert.Equal(["system", "prune", "-a", "--volumes", "--force"], runner.Invocations[0].Arguments);
    }

    [Fact]
    public async Task VagrantCleaner_keeps_boxes()
    {
        var fs = new FakeFileSystem()
            .AddFile("/home/test/.vagrant.d/tmp/partial-download", 3_000)
            .AddFile("/home/test/.vagrant.d/boxes/ubuntu/box.img", 9_999);
        var env = new FakeEnvironment { HomeDirectory = "/home/test", Os = OsPlatform.Linux };

        var result = await new VagrantCleaner().CleanAsync(TestContext.Create(fs, env));

        Assert.Equal(3_000, result.BytesFreed);
        Assert.True(fs.FileExists("/home/test/.vagrant.d/boxes/ubuntu/box.img"));
    }
}

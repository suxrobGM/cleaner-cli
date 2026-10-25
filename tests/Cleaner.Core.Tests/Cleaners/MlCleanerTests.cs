using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Applications;
using Cleaner.Core.Cleaners.DevTools;
using Cleaner.Core.Cleaners.Os;
using Cleaner.Core.Services;
using Cleaner.Core.Tests.Fakes;
using Xunit;

namespace Cleaner.Core.Tests;

public sealed class MlCleanerTests
{
    [Fact]
    public async Task MlCacheCleaner_clears_huggingface_and_torch_under_dot_cache()
    {
        var fs = new FakeFileSystem()
            .AddFile("/home/test/.cache/huggingface/hub/model.bin", 8_000)
            .AddFile("/home/test/.cache/torch/hub/weights.pt", 2_000)
            .AddFile("/home/test/.cache/keep/other.txt", 99);
        var env = new FakeEnvironment { HomeDirectory = "/home/test", Os = OsPlatform.Linux };

        var result = await new MlCacheCleaner().CleanAsync(TestContext.Create(fs, env));

        Assert.Equal(10_000, result.BytesFreed);
        Assert.True(fs.FileExists("/home/test/.cache/keep/other.txt"));
    }

    [Fact]
    public async Task MlCacheCleaner_honors_HF_HOME_override()
    {
        var fs = new FakeFileSystem().AddFile("/data/hf/hub/model.bin", 5_000);
        var env = new FakeEnvironment { HomeDirectory = "/home/test", Os = OsPlatform.Linux }
            .SetVariable("HF_HOME", "/data/hf");

        var result = await new MlCacheCleaner().ScanAsync(TestContext.Create(fs, env));

        Assert.Equal(5_000, result.TotalBytes);
    }

    [Fact]
    public async Task MlCacheCleaner_clears_huggingface_caches_but_keeps_the_login()
    {
        const string hf = "/home/test/.cache/huggingface";
        var fs = new FakeFileSystem()
            .AddFile($"{hf}/hub/models--gpt2/blobs/a", 1_000)
            .AddFile($"{hf}/xet/chunks/b", 2_000)
            .AddFile($"{hf}/datasets/squad/c", 3_000)
            .AddFile($"{hf}/token", 40)
            .AddFile($"{hf}/stored_tokens", 80);
        var env = new FakeEnvironment { HomeDirectory = "/home/test", Os = OsPlatform.Linux };

        var result = await new MlCacheCleaner().CleanAsync(TestContext.Create(fs, env));

        Assert.Equal(6_000, result.BytesFreed);
        Assert.True(fs.FileExists($"{hf}/token"));
        Assert.True(fs.FileExists($"{hf}/stored_tokens"));
    }

    [Fact]
    public async Task MlCacheCleaner_honors_relocated_hub_and_datasets_caches()
    {
        var fs = new FakeFileSystem()
            .AddFile("/mnt/hub/models--gpt2/blobs/a", 1_000)
            .AddFile("/mnt/datasets/squad/b", 2_000)
            .AddFile("/mnt/legacy/models--bert/blobs/c", 3_000);
        var env = new FakeEnvironment { HomeDirectory = "/home/test", Os = OsPlatform.Linux }
            .SetVariable("HF_HUB_CACHE", "/mnt/hub")
            .SetVariable("HF_DATASETS_CACHE", "/mnt/datasets")
            .SetVariable("TRANSFORMERS_CACHE", "/mnt/legacy");

        var result = await new MlCacheCleaner().ScanAsync(TestContext.Create(fs, env));

        Assert.Equal(6_000, result.TotalBytes);
    }
}

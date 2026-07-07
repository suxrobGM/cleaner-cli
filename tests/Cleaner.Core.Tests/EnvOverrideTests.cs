using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.DevTools;
using Cleaner.Core.Services;
using Cleaner.Core.Tests.Fakes;
using Xunit;

namespace Cleaner.Core.Tests;

/// <summary>
/// Cleaners must honor the env vars that relocate a tool's cache; otherwise scans under-report and
/// direct deletion targets a location the tool no longer uses.
/// </summary>
public sealed class EnvOverrideTests
{
    /// <summary>(cleaner, variable, configured value, directory the cleaner must then target).</summary>
    public static TheoryData<Func<ICleaner>, string, string, string> Overrides => new()
    {
        { () => new NuGetCleaner(), "NUGET_PACKAGES", "/mnt/nuget", "/mnt/nuget" },
        { () => new CargoCleaner(), "CARGO_HOME", "/mnt/cargo", "/mnt/cargo/registry/cache" },
        { () => new RustupCleaner(), "RUSTUP_HOME", "/mnt/rustup", "/mnt/rustup/downloads" },
        { () => new GoCleaner(), "GOMODCACHE", "/mnt/gomod", "/mnt/gomod" },
        { () => new GradleCleaner(), "GRADLE_USER_HOME", "/mnt/gradle", "/mnt/gradle/caches" },
        { () => new NpmCleaner(), "npm_config_cache", "/mnt/npm", "/mnt/npm/_cacache" },
        { () => new NpxCleaner(), "npm_config_cache", "/mnt/npm", "/mnt/npm/_npx" },
        { () => new YarnCleaner(), "YARN_CACHE_FOLDER", "/mnt/yarn", "/mnt/yarn" },
        { () => new BunCleaner(), "BUN_INSTALL_CACHE_DIR", "/mnt/bun-cache", "/mnt/bun-cache" },
        { () => new BunCleaner(), "BUN_INSTALL", "/mnt/bun", "/mnt/bun/install/cache" },
        { () => new PipCleaner(), "PIP_CACHE_DIR", "/mnt/pip", "/mnt/pip" },
        { () => new PoetryCleaner(), "POETRY_CACHE_DIR", "/mnt/poetry", "/mnt/poetry" },
        { () => new UvCleaner(), "UV_CACHE_DIR", "/mnt/uv", "/mnt/uv" },
        { () => new PubCleaner(), "PUB_CACHE", "/mnt/pub", "/mnt/pub/hosted" },
    };

    [Theory]
    [MemberData(nameof(Overrides))]
    public async Task Cleaner_scans_the_relocated_cache(
        Func<ICleaner> factory, string variable, string configured, string expectedTarget)
    {
        var fs = new FakeFileSystem().AddFile($"{expectedTarget}/payload.bin", 4_000);
        var env = new FakeEnvironment { Os = OsPlatform.Linux }.SetVariable(variable, configured);

        var result = await factory().ScanAsync(TestContext.Create(fs, env));

        Assert.Equal(4_000, result.TotalBytes);
    }

    [Fact]
    public async Task GoCleaner_prefers_GOMODCACHE_over_GOPATH()
    {
        var fs = new FakeFileSystem()
            .AddFile("/mnt/gomod/pkg.zip", 1_000)
            .AddFile("/gopath/pkg/mod/other.zip", 9_999);
        var env = new FakeEnvironment { Os = OsPlatform.Linux }
            .SetVariable("GOPATH", "/gopath")
            .SetVariable("GOMODCACHE", "/mnt/gomod");

        var result = await new GoCleaner().ScanAsync(TestContext.Create(fs, env));

        Assert.Contains(result.Targets, t => t.Path.Replace('\\', '/') == "/mnt/gomod");
        Assert.DoesNotContain(result.Targets, t => t.Path.Replace('\\', '/').Contains("/gopath"));
    }
}

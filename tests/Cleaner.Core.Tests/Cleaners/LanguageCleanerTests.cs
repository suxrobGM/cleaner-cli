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
            .AddFile($@"{local}\vcpkg\installed\lib.a", 9_999);
        var env = new FakeEnvironment { Os = OsPlatform.Windows, LocalAppDataDirectory = local };

        var result = await new VcpkgCleaner().CleanAsync(TestContext.Create(fs, env));

        Assert.Equal(3_000, result.BytesFreed);
        Assert.True(fs.FileExists($@"{local}\vcpkg\installed\lib.a"));
    }

    [Fact]
    public async Task ConanCleaner_never_deletes_the_package_store_directly()
    {
        // Locally built or exported packages live here and no remote can restore them.
        var fs = new FakeFileSystem()
            .AddFile("/mnt/conan/p/zlib1234/p/lib/zlib.a", 4_000)
            .AddFile("/home/test/.conan/data/zlib/1.0/package/lib.a", 4_000);
        var env = new FakeEnvironment { HomeDirectory = "/home/test", Os = OsPlatform.Linux }
            .SetVariable("CONAN_HOME", "/mnt/conan");
        var context = TestContext.Create(fs, env);
        var cleaner = new ConanCleaner();

        var result = await cleaner.CleanAsync(context);

        Assert.False(cleaner.IsAvailable(context));
        Assert.Equal(0, result.BytesFreed);
        Assert.True(fs.FileExists("/mnt/conan/p/zlib1234/p/lib/zlib.a"));
        Assert.True(fs.FileExists("/home/test/.conan/data/zlib/1.0/package/lib.a"));
    }

    [Fact]
    public async Task ConanCleaner_only_runs_cache_clean()
    {
        var runner = new FakeProcessRunner().WithAvailable("conan");
        var fs = new FakeFileSystem().AddFile("/home/test/.conan2/p/pkg/file", 100);
        var env = new FakeEnvironment { HomeDirectory = "/home/test", Os = OsPlatform.Linux };
        var context = TestContext.Create(fs, env, runner);
        var cleaner = new ConanCleaner();

        await cleaner.CleanAsync(context);

        Assert.True(cleaner.IsAvailable(context));
        Assert.False(cleaner.SupportsSizeEstimate);
        var commands = runner.Invocations.Select(i => string.Join(' ', i.Arguments)).ToList();
        Assert.Equal(["cache clean *"], commands);
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

    [Fact]
    public async Task RubyGemsCleaner_clears_the_spec_cache_without_uninstalling_gems()
    {
        // gem cleanup would remove older versions a Gemfile.lock may still pin.
        var runner = new FakeProcessRunner().WithAvailable("gem");
        var fs = new FakeFileSystem()
            .AddFile("/home/test/.gem/specs/rubygems.org%443/specs.4.8", 3_000)
            .AddFile("/home/test/.gem/ruby/3.3.0/gems/rails-7.0.0/lib/rails.rb", 9_999);
        var env = new FakeEnvironment { HomeDirectory = "/home/test", Os = OsPlatform.Linux };

        var result = await new RubyGemsCleaner().CleanAsync(TestContext.Create(fs, env, runner));

        Assert.Equal(3_000, result.BytesFreed);
        Assert.Empty(runner.Invocations);
        Assert.True(fs.DirectoryExists("/home/test/.gem/specs"));
        Assert.True(fs.FileExists("/home/test/.gem/ruby/3.3.0/gems/rails-7.0.0/lib/rails.rb"));
    }

    [Fact]
    public async Task HexMixCleaner_keeps_installed_mix_archives()
    {
        var fs = new FakeFileSystem()
            .AddFile("/home/test/.hex/packages/hexpm/jason-1.4.0.tar", 2_000)
            .AddFile("/home/test/.mix/archives/hex-2.0.0/hex-2.0.0/ebin/hex.app", 9_999);
        var env = new FakeEnvironment { HomeDirectory = "/home/test", Os = OsPlatform.Linux };

        var result = await new HexMixCleaner().CleanAsync(TestContext.Create(fs, env));

        Assert.Equal(2_000, result.BytesFreed);
        Assert.True(fs.FileExists("/home/test/.mix/archives/hex-2.0.0/hex-2.0.0/ebin/hex.app"));
    }

    [Fact]
    public async Task NimCleaner_uses_the_xdg_cache_on_macos()
    {
        var fs = new FakeFileSystem()
            .AddFile("/Users/test/.cache/nim/app_d/@mapp.nim.c", 1_500)
            .AddFile("/Users/test/Library/Caches/nim/other.bin", 9_999);
        var env = new FakeEnvironment { HomeDirectory = "/Users/test", Os = OsPlatform.MacOs };

        var result = await new NimCleaner().CleanAsync(TestContext.Create(fs, env));

        Assert.Equal(1_500, result.BytesFreed);
        Assert.True(fs.FileExists("/Users/test/Library/Caches/nim/other.bin"));
    }

    [Fact]
    public async Task NimCleaner_uses_the_home_nimcache_on_windows()
    {
        var fs = new FakeFileSystem()
            .AddFile(@"C:\Users\test\nimcache\app_r\@mapp.nim.c", 1_500)
            .AddFile(@"C:\Users\test\AppData\Local\nim\nimble.ini", 9_999);

        var result = await new NimCleaner().CleanAsync(TestContext.Create(fs, FakeEnvironment.Windows()));

        Assert.Equal(1_500, result.BytesFreed);
        Assert.True(fs.FileExists(@"C:\Users\test\AppData\Local\nim\nimble.ini"));
    }
}

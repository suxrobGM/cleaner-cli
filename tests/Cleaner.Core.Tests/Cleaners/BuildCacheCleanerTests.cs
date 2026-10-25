using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.DevTools;
using Cleaner.Core.Tests.Fakes;
using Xunit;

namespace Cleaner.Core.Tests;

public sealed class BuildCacheCleanerTests
{
    private static async Task<List<string>> ScanAsync(FakeFileSystem fs, params string[] roots)
    {
        var result = await new BuildArtifactCleaner().ScanAsync(TestContext.Create(fs, scanRoots: roots));
        return [.. result.Targets.Select(t => t.Path.Replace('\\', '/'))];
    }

    [Fact]
    public async Task BuildArtifactCleaner_takes_dotnet_bin_and_obj_only_from_a_restored_project()
    {
        var fs = new FakeFileSystem()
            .AddFile("/work/app/App.csproj", 1)
            .AddFile("/work/app/obj/project.assets.json", 10)
            .AddFile("/work/app/bin/Debug/net10.0/App.dll", 100)
            .AddFile("/work/tools/bin/tool.exe", 500)
            .AddFile("/work/scripts/bin/deploy.ps1", 5);

        var paths = await ScanAsync(fs, "/work");

        Assert.Equal(["/work/app/bin", "/work/app/obj"], paths.Order());
    }

    [Fact]
    public async Task BuildArtifactCleaner_takes_platform_specific_dotnet_output()
    {
        var fs = new FakeFileSystem()
            .AddFile("/work/app/App.csproj", 1)
            .AddFile("/work/app/obj/project.assets.json", 10)
            .AddFile("/work/app/bin/x64/Release/App.dll", 100);

        Assert.Contains("/work/app/bin", await ScanAsync(fs, "/work"));
    }

    [Fact]
    public async Task BuildArtifactCleaner_takes_node_modules_only_beside_a_lockfile()
    {
        var fs = new FakeFileSystem()
            .AddFile("/work/web/package.json", 1)
            .AddFile("/work/web/package-lock.json", 1)
            .AddFile("/work/web/node_modules/react/index.js", 100)
            // Shaped like an editor extension or a global install: no lockfile, no install stamp.
            .AddFile("/work/vendor-ext/package.json", 1)
            .AddFile("/work/vendor-ext/node_modules/minimatch/index.js", 100);

        Assert.Equal(["/work/web/node_modules"], await ScanAsync(fs, "/work"));
    }

    [Fact]
    public async Task BuildArtifactCleaner_takes_dist_only_when_the_project_ignores_it()
    {
        var fs = new FakeFileSystem()
            .AddDirectory("/work/.git")
            .AddFile("/work/.gitignore", "node_modules/\n/dist\n")
            .AddFile("/work/package.json", 1)
            .AddFile("/work/dist/bundle.js", 100)
            .AddFile("/work/lib/package.json", 1)
            .AddFile("/work/lib/dist/committed.js", 100);

        // "/dist" is anchored to the repository root, so it says nothing about /work/lib/dist.
        Assert.Equal(["/work/dist"], await ScanAsync(fs, "/work"));
    }

    [Fact]
    public async Task BuildArtifactCleaner_respects_a_gitignore_negation()
    {
        var fs = new FakeFileSystem()
            .AddDirectory("/work/.git")
            .AddFile("/work/.gitignore", "dist/\n!dist/\n")
            .AddFile("/work/package.json", 1)
            .AddFile("/work/dist/bundle.js", 100);

        Assert.Empty(await ScanAsync(fs, "/work"));
    }

    [Fact]
    public async Task BuildArtifactCleaner_ignores_dist_without_a_gitignore_entry()
    {
        var fs = new FakeFileSystem()
            .AddFile("/work/package.json", 1)
            .AddFile("/work/dist/bundle.js", 100);

        Assert.Empty(await ScanAsync(fs, "/work"));
    }

    [Fact]
    public async Task BuildArtifactCleaner_takes_cargo_target_only_with_its_cache_tag()
    {
        var fs = new FakeFileSystem()
            .AddFile("/work/crate/Cargo.toml", 1)
            .AddFile("/work/crate/target/CACHEDIR.TAG", 1)
            .AddFile("/work/crate/target/debug/app", 100)
            .AddFile("/work/other/Cargo.toml", 1)
            .AddFile("/work/other/target/notes.txt", 5);

        Assert.Equal(["/work/crate/target"], await ScanAsync(fs, "/work"));
    }

    [Fact]
    public async Task BuildArtifactCleaner_takes_virtualenvs_only_inside_python_projects()
    {
        var fs = new FakeFileSystem()
            .AddFile("/work/api/pyproject.toml", 1)
            .AddFile("/work/api/.venv/pyvenv.cfg", 1)
            .AddFile("/work/api/.venv/lib/site-packages/torch.so", 5_000)
            .AddFile("/work/cli/requirements-dev.txt", 1)
            .AddFile("/work/cli/venv/pyvenv.cfg", 1)
            .AddFile("/work/loose/venv/pyvenv.cfg", 1);

        Assert.Equal(["/work/api/.venv", "/work/cli/venv"], (await ScanAsync(fs, "/work")).Order());
    }

    [Fact]
    public async Task BuildArtifactCleaner_takes_build_only_beside_a_build_system_with_proof()
    {
        var fs = new FakeFileSystem()
            .AddFile("/work/native/CMakeLists.txt", 10)
            .AddFile("/work/native/build/CMakeCache.txt", 2_000)
            .AddFile("/work/android/build.gradle", 10)
            .AddFile("/work/android/build/tmp/x", 4_000)
            .AddFile("/work/docs/build/index.html", 9_999)
            .AddFile("/work/scripts/CMakeLists.txt", 1)
            .AddFile("/work/scripts/build/release.sh", 9);

        Assert.Equal(["/work/android/build", "/work/native/build"], (await ScanAsync(fs, "/work")).Order());
    }

    [Fact]
    public async Task BuildArtifactCleaner_refuses_to_scan_the_profile_and_says_how_to_fix_it()
    {
        var fs = new FakeFileSystem()
            .AddFile("/home/test/proj/App.csproj", 1)
            .AddFile("/home/test/proj/obj/project.assets.json", 1);

        var result = await new BuildArtifactCleaner().ScanAsync(TestContext.Create(fs, scanRoots: ["/home/test"]));

        Assert.Empty(result.Targets);
        Assert.Contains(result.Notes, n => n.Contains("--path"));
    }

    [Fact]
    public async Task BuildArtifactCleaner_refuses_a_drive_root_and_system_folders()
    {
        var env = FakeEnvironment.Windows();
        var context = TestContext.Create(new FakeFileSystem(), env, scanRoots: [@"C:\", @"C:\Windows\System32", @"C:\Users\test\AppData\Local\nvm"]);

        var result = await new BuildArtifactCleaner().ScanAsync(context);

        Assert.Equal(3, result.Notes.Count);
    }

    [Fact]
    public async Task BuildArtifactCleaner_never_follows_links_or_enters_dot_folders()
    {
        var fs = new FakeFileSystem()
            .AddFile("/elsewhere/App.csproj", 1)
            .AddFile("/elsewhere/obj/project.assets.json", 1)
            .AddLink("/work/current", "/elsewhere")
            .AddFile("/work/.hidden/App.csproj", 1)
            .AddFile("/work/.hidden/obj/project.assets.json", 1);

        Assert.Empty(await ScanAsync(fs, "/work"));
    }

    [Fact]
    public async Task BuildArtifactCleaner_scans_every_root()
    {
        var fs = new FakeFileSystem()
            .AddFile("/r1/proj/package.json", 1)
            .AddFile("/r1/proj/yarn.lock", 1)
            .AddFile("/r1/proj/node_modules/pkg/index.js", 100)
            .AddFile("/r2/app/Cargo.toml", 1)
            .AddFile("/r2/app/target/CACHEDIR.TAG", 200);

        var result = await new BuildArtifactCleaner().ScanAsync(TestContext.Create(fs, scanRoots: ["/r1", "/r2"]));

        Assert.Equal(300, result.TotalBytes);
    }

    [Fact]
    public async Task BuildArtifactCleaner_deletes_only_what_it_found()
    {
        var fs = new FakeFileSystem()
            .AddFile("/work/app/App.csproj", 1)
            .AddFile("/work/app/obj/project.assets.json", 10)
            .AddFile("/work/app/Program.cs", 1);

        var result = await new BuildArtifactCleaner().CleanAsync(TestContext.Create(fs, scanRoots: ["/work"]));

        Assert.Equal(10, result.BytesFreed);
        Assert.True(fs.FileExists("/work/app/Program.cs"));
        Assert.False(fs.DirectoryExists("/work/app/obj"));
    }

    [Fact]
    public void BuildArtifactCleaner_is_opt_in()
    {
        ICleaner cleaner = new BuildArtifactCleaner();
        Assert.True(cleaner.OptIn);
        Assert.NotNull(cleaner.ConfirmationWarning);
    }
}

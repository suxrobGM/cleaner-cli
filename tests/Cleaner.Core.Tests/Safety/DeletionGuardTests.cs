using Cleaner.Core.Services;
using Cleaner.Core.Tests.Fakes;
using Xunit;

namespace Cleaner.Core.Tests.Safety;

public sealed class DeletionGuardTests
{
    private const string H = @"C:\Users\test";
    private const string L = @"C:\Users\test\AppData\Local";

    private static DeletionGuard Guard(FakeEnvironment? env = null, FakeFileSystem? fs = null, FakeProcessRunner? runner = null) =>
        DeletionGuard.Create(env ?? FakeEnvironment.Windows(), fs ?? new FakeFileSystem(), runner ?? new FakeProcessRunner());

    [Theory]
    [InlineData(@"C:\")]
    [InlineData(H)]
    [InlineData(@"C:\Users")]
    [InlineData(L)]
    [InlineData(@"C:\Users\test\AppData")]
    [InlineData(@"C:\Windows")]
    [InlineData(@"C:\Windows\System32")]
    [InlineData(@"C:\Program Files")]
    [InlineData(@"C:\Users\test\Documents")]
    public void Essential_folders_and_their_parents_are_protected(string path) =>
        Assert.NotNull(Guard().WhyProtected(path));

    [Fact]
    public void A_path_on_PATH_its_parents_and_its_contents_are_protected()
    {
        var env = FakeEnvironment.Windows();
        env.PathDirectories = [$@"{H}\.aspire\bin"];
        var guard = Guard(env);

        Assert.Equal("is on PATH", guard.WhyProtected($@"{H}\.aspire\bin"));
        Assert.Contains("contains the PATH folder", guard.WhyProtected($@"{H}\.aspire"));
        Assert.Contains("inside the PATH folder", guard.WhyProtected($@"{H}\.aspire\bin\sub"));
        Assert.Null(guard.WhyProtected($@"{H}\.aspire-cache"));
    }

    [Fact]
    public void Inside_the_Windows_folder_on_PATH_caches_stay_cleanable()
    {
        var env = FakeEnvironment.Windows();
        env.PathDirectories = [@"C:\Windows", @"C:\Windows\system32", @"C:\"];
        var guard = Guard(env);

        Assert.Null(guard.WhyProtected(@"C:\Windows\Temp"));
        Assert.Null(guard.WhyProtected(@"C:\Windows\SoftwareDistribution\Download"));
        Assert.Null(guard.WhyProtected(@"C:\NVIDIA"));
    }

    [Fact]
    public void A_linked_PATH_entry_protects_the_folder_it_points_at()
    {
        var env = FakeEnvironment.Windows();
        env.PathDirectories = [$@"{L}\nvm\nodejs"];
        var fs = new FakeFileSystem().AddLink($@"{L}\nvm\nodejs", @"D:\node\v24.21.0");

        Assert.NotNull(Guard(env, fs).WhyProtected(@"D:\node\v24.21.0\node_modules"));
    }

    [Theory]
    [InlineData(@"C:\Users\test\.cargo\bin")]
    [InlineData(@"C:\Users\test\.cargo\registry\index")]
    [InlineData(@"C:\Users\test\.rustup\toolchains\stable-x86_64-pc-windows-msvc\bin")]
    [InlineData(@"C:\Users\test\scoop\apps\make\4.4.1\bin")]
    [InlineData(@"C:\Users\test\scoop\apps")]
    [InlineData(@"C:\Users\test\AppData\Local\Microsoft\WinGet\Packages\Gyan.FFmpeg_x\ffmpeg\bin")]
    [InlineData(@"C:\Users\test\AppData\Local\Author Software\nvm\installs\v24.21.0\node_modules")]
    [InlineData(@"C:\Users\test\.local\bin")]
    [InlineData(@"C:\Users\test\bin")]
    [InlineData(@"C:\Users\test\.bun\bin")]
    [InlineData(@"C:\Users\test\.vscode\extensions\kennethguo.vscode-counter-3.8.1\node_modules")]
    [InlineData(@"C:\Users\test\AppData\Local\Programs\Ollama")]
    [InlineData(@"C:\Program Files\Microsoft Visual Studio\18\Community\VC\vcpkg")]
    [InlineData(@"C:\Program Files (x86)\Windows Kits\10\bin\10.0.26100.0")]
    [InlineData(@"C:\Users\test\.dotnet\sdk")]
    [InlineData(@"C:\Users\test\.ssh")]
    public void Installed_tools_are_protected_without_any_PATH_entry(string path) =>
        Assert.NotNull(Guard().WhyProtected(path));

    [Theory]
    [InlineData(@"C:\Users\test\.cargo\registry\cache")]
    [InlineData(@"C:\Users\test\.cargo\registry\src\index.crates.io\serde-1.0.0")]
    [InlineData(@"C:\Users\test\.cargo\git\checkouts")]
    [InlineData(@"C:\Users\test\.rustup\downloads")]
    [InlineData(@"C:\Users\test\.bun\install\cache")]
    [InlineData(@"C:\Users\test\scoop\cache")]
    [InlineData(@"C:\Users\test\AppData\Local\pnpm\store")]
    [InlineData(@"C:\Users\test\.dotnet\TelemetryStorageService")]
    [InlineData(@"C:\Users\test\.gradle\caches")]
    [InlineData(@"C:\Users\test\.gradle\wrapper\dists")]
    [InlineData(@"C:\Users\test\.android\cache")]
    [InlineData(@"C:\Program Files (x86)\Steam\steamapps\shadercache")]
    [InlineData(@"C:\Users\test\AppData\Local\npm-cache\_cacache")]
    [InlineData(@"C:\Users\test\AppData\Local\Temp\junk")]
    public void Caches_inside_installs_stay_cleanable(string path) =>
        Assert.Null(Guard().WhyProtected(path));

    [Fact]
    public void Install_roots_follow_their_environment_overrides()
    {
        var env = FakeEnvironment.Windows().SetVariable("CARGO_HOME", @"D:\rust\cargo");

        Assert.NotNull(Guard(env).WhyProtected(@"D:\rust\cargo\bin"));
        Assert.Null(Guard(env).WhyProtected(@"D:\rust\cargo\registry\cache"));
    }

    [Fact]
    public void Git_folders_and_repositories_are_protected_but_clones_inside_caches_are_not()
    {
        var fs = new FakeFileSystem()
            .AddDirectory(@"D:\src\app\.git")
            .AddDirectory(@"D:\cache\pre-commit\repo1\.git");
        var guard = Guard(fs: fs);

        Assert.Equal("is a git repository", guard.WhyProtected(@"D:\src\app"));
        Assert.Equal("is inside a .git folder", guard.WhyProtected(@"D:\src\app\.git\objects"));
        Assert.Null(guard.WhyProtected(@"D:\cache\pre-commit"));
        Assert.Null(guard.WhyProtected(@"D:\src\app\bin"));
    }

    [Fact]
    public void A_target_reached_through_a_link_is_judged_by_the_real_folder()
    {
        var fs = new FakeFileSystem().AddLink(@"D:\work\tools", $@"{H}\.cargo");

        Assert.Contains("resolves to", Guard(fs: fs).WhyProtected(@"D:\work\tools\bin"));
    }

    [Fact]
    public void A_link_itself_is_never_a_target()
    {
        var fs = new FakeFileSystem().AddLink(@"D:\work\current", @"D:\work\v2");

        Assert.Contains("is a link", Guard(fs: fs).WhyProtected(@"D:\work\current"));
    }

    [Fact]
    public void The_user_exclude_file_adds_protection()
    {
        var env = FakeEnvironment.Windows().SetVariable("WORK", @"D:\work");
        var fs = new FakeFileSystem().AddFile($@"{H}\.cleaner\exclude.txt", "# keep these\n~/models\n%WORK%\\golden\n\nrelative\\ignored\n");
        var guard = Guard(env, fs);

        Assert.Contains("excluded", guard.WhyProtected($@"{H}\models\llama"));
        Assert.Contains("excluded", guard.WhyProtected(@"D:\work\golden"));
        Assert.Contains("excluded", guard.WhyProtected(@"D:\work"));
        Assert.Null(guard.WhyProtected(@"D:\work\other"));
    }

    [Fact]
    public void Clearing_a_folder_that_contains_an_exclusion_is_judged_per_child()
    {
        var guard = Guard().Excluding([@"D:\cache\keep"]);

        Assert.Null(guard.WhyProtected(@"D:\cache", contentsOnly: true));
        Assert.NotNull(guard.WhyProtected(@"D:\cache"));
        Assert.NotNull(guard.WhyProtected(@"D:\cache\keep"));
        Assert.Null(guard.WhyProtected(@"D:\cache\drop"));
    }

    [Fact]
    public void A_running_program_marks_its_folder_in_use()
    {
        var runner = new FakeProcessRunner();
        runner.RunningExecutables.Add(@"D:\tools\app\app.exe");
        var guard = Guard(runner: runner);

        Assert.NotNull(guard.WhyInUse(@"D:\tools\app"));
        Assert.NotNull(guard.WhyInUse(@"D:\tools"));
        Assert.Null(guard.WhyInUse(@"D:\other"));
    }

    [Theory]
    [InlineData(H)]
    [InlineData(@"C:\")]
    [InlineData(@"C:\Users")]
    [InlineData(@"C:\Windows\System32")]
    [InlineData(@"C:\Program Files (x86)\Windows Kits")]
    [InlineData(@"C:\Users\test\AppData\Local\nvm")]
    [InlineData(@"C:\Users\test\scoop\apps")]
    [InlineData("relative")]
    public void Project_roots_cannot_be_system_profile_or_install_folders(string root) =>
        Assert.NotNull(Guard().WhyNotProjectRoot(root));

    [Theory]
    [InlineData(@"C:\Users\test\source\repos")]
    [InlineData(@"D:\projects")]
    public void Project_roots_can_be_workspaces(string root) =>
        Assert.Null(Guard().WhyNotProjectRoot(root));

    [Fact]
    public void Project_search_never_enters_dot_folders_node_modules_or_installs()
    {
        var guard = Guard();

        Assert.False(guard.CanEnter(@"D:\work\.vscode"));
        Assert.False(guard.CanEnter(@"D:\work\node_modules"));
        Assert.False(guard.CanEnter($@"{H}\scoop\apps"));
        Assert.True(guard.CanEnter(@"D:\work\src"));
    }

    [Fact]
    public void Relative_paths_are_never_deletable() =>
        Assert.NotNull(Guard().WhyProtected(@"cache\x"));

    [Fact]
    public void Linux_paths_are_compared_case_sensitively()
    {
        var env = new FakeEnvironment { PathDirectories = ["/home/test/tools/bin"] };
        var guard = Guard(env);

        Assert.NotNull(guard.WhyProtected("/home/test/tools"));
        Assert.Null(guard.WhyProtected("/home/test/Tools"));
    }
}

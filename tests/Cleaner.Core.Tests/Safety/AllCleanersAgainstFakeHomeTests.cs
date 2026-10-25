using Cleaner.Cli.Infrastructure;
using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners;
using Cleaner.Core.Services;
using Cleaner.Core.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cleaner.Core.Tests.Safety;

/// <summary>
/// Runs every registered cleaner against <see cref="FakeHome"/> and checks that only caches and
/// project build output are deleted.
/// </summary>
public sealed class AllCleanersAgainstFakeHomeTests
{
    private static IReadOnlyList<ICleaner> AllCleaners()
    {
        var services = new ServiceCollection();
        services.AddCleaner();
        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<ICleanerRegistry>().All;
    }

    /// <summary>Scans and cleans every applicable cleaner, like "All cleaners" plus the project scan.</summary>
    private static async Task RunAllAsync(FakeHome home, FakeProcessRunner runner, string workingDirectory, string? scanRoot = null)
    {
        var context = TestContext.Create(
            home.FileSystem,
            home.Environment,
            runner,
            workingDirectory: workingDirectory,
            scanRoots: scanRoot is null ? null : [scanRoot]);
        var logger = new NullAppLogger();

        foreach (var cleaner in AllCleaners().Where(c => c.IsApplicable(context)))
        {
            var scan = await CleanerRunner.SafeScanAsync(cleaner, context, logger);
            await CleanerRunner.SafeCleanAsync(cleaner, context, scan.Targets, logger);
        }
    }

    public static TheoryData<string> Homes => new() { "windows", "linux" };

    private static FakeHome Build(string kind) => kind == "windows" ? FakeHome.Windows() : FakeHome.Linux();

    [Theory]
    [MemberData(nameof(Homes))]
    public async Task Started_from_home_every_install_survives_and_caches_go(string kind)
    {
        var home = Build(kind);
        var onPathBefore = home.FilesOnPath();

        // No tool answers, so every cleaner deletes its folders directly, where a wrong target does most damage.
        await RunAllAsync(home, new FakeProcessRunner(), workingDirectory: home.Home);

        Assert.All(home.Installed, file => Assert.True(home.FileSystem.FileExists(file), $"deleted an install: {file}"));
        Assert.All(onPathBefore, file => Assert.True(home.FileSystem.FileExists(file), $"deleted a file on PATH: {file}"));

        // The project scan refuses to run from the profile, so build output stays.
        Assert.All(home.ProjectArtifacts, file => Assert.True(home.FileSystem.FileExists(file), $"removed build output from a home scan: {file}"));
        Assert.All(home.Caches, file => Assert.False(home.FileSystem.FileExists(file), $"left a cache behind: {file}"));
    }

    [Theory]
    [MemberData(nameof(Homes))]
    public async Task Pointed_at_the_projects_folder_only_build_output_and_caches_go(string kind)
    {
        var home = Build(kind);
        var onPathBefore = home.FilesOnPath();

        await RunAllAsync(home, new FakeProcessRunner(), workingDirectory: home.Repos, scanRoot: home.Repos);

        Assert.All(home.Installed, file => Assert.True(home.FileSystem.FileExists(file), $"deleted an install: {file}"));
        Assert.All(onPathBefore, file => Assert.True(home.FileSystem.FileExists(file), $"deleted a file on PATH: {file}"));
        Assert.All(home.ProjectArtifacts, file => Assert.False(home.FileSystem.FileExists(file), $"left build output: {file}"));
    }

    [Fact]
    public async Task With_every_tool_installed_the_tools_clean_up_and_no_install_is_touched()
    {
        var home = FakeHome.Windows();
        var runner = new FakeProcessRunner().WithAvailable(
            "npm", "pnpm", "yarn", "bun", "uv", "pip", "dotnet", "scoop", "go", "conda", "docker", "mise", "pre-commit");

        await RunAllAsync(home, runner, workingDirectory: home.Home);

        Assert.All(home.Installed, file => Assert.True(home.FileSystem.FileExists(file), $"deleted an install: {file}"));
        Assert.Contains(runner.Invocations, i => i.Executable == "bun" && i.Arguments.SequenceEqual(["pm", "cache", "rm"]));
        Assert.Contains(runner.Invocations, i => i.Executable == "scoop" && i.Arguments.SequenceEqual(["cleanup", "*"]));
        Assert.DoesNotContain(runner.Invocations, i => i.Arguments.Contains("--volumes"));
    }

    [Fact]
    public async Task A_running_program_keeps_its_folder()
    {
        var home = FakeHome.Windows();
        var temp = home.Environment.TempDirectory;
        home.FileSystem.AddFile($@"{temp}\vscode-update\Code.exe", 100);
        var runner = new FakeProcessRunner();
        runner.RunningExecutables.Add($@"{temp}\vscode-update\Code.exe");

        await RunAllAsync(home, runner, workingDirectory: home.Home);

        Assert.True(home.FileSystem.FileExists($@"{temp}\vscode-update\Code.exe"));
        Assert.False(home.FileSystem.FileExists($@"{temp}\junk\setup.log"));
    }

    [Fact]
    public async Task A_package_with_an_open_file_is_left_whole()
    {
        var home = FakeHome.Windows();
        var temp = home.Environment.TempDirectory;
        home.FileSystem.AddFile($@"{temp}\pkg\bin\tool.exe", 100).AddFile($@"{temp}\pkg\doc\readme.txt", 100);
        home.FileSystem.InUse.Add($@"{temp}\pkg\bin\tool.exe");

        await RunAllAsync(home, new FakeProcessRunner(), workingDirectory: home.Home);

        // Rename-then-delete: the rename fails while a file is open, so nothing inside is touched.
        Assert.True(home.FileSystem.FileExists($@"{temp}\pkg\bin\tool.exe"));
        Assert.True(home.FileSystem.FileExists($@"{temp}\pkg\doc\readme.txt"));
    }

    [Fact]
    public void Every_cleaner_target_on_a_clean_machine_passes_the_guard_or_is_reported()
    {
        // A guard that blocks real caches would make cleaners useless without anyone noticing.
        var home = FakeHome.Windows();
        var context = TestContext.Create(home.FileSystem, home.Environment, workingDirectory: home.Home);

        Assert.All(home.Caches, cache =>
        {
            var folder = Path.GetDirectoryName(cache.Replace('\\', Path.DirectorySeparatorChar))!;
            Assert.Null(context.Guard.WhyProtected(folder));
        });
    }
}

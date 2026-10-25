using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Cleaners.DevTools;
using Cleaner.Core.Services;
using Cleaner.Core.Tests.Fakes;
using Xunit;

namespace Cleaner.Core.Tests;

/// <summary>Exercises real-file behavior that the in-memory fake cannot model.</summary>
public sealed class FileSystemServiceTests : IDisposable
{
    private readonly FileSystemService _fs = new();
    private readonly string _root = Path.Combine(Path.GetTempPath(), "cleaner-fs-tests-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void DeleteDirectory_removes_tree_containing_read_only_files()
    {
        // Package caches can contain read-only files on Windows.
        var dir = Path.Combine(_root, "cache");
        var file = Path.Combine(dir, "locked.dll");
        Directory.CreateDirectory(dir);
        File.WriteAllText(file, "payload");
        File.SetAttributes(file, FileAttributes.ReadOnly);

        _fs.DeleteDirectory(dir);

        Assert.False(Directory.Exists(dir));
    }

    [Fact]
    public void DeleteDirectory_does_not_traverse_into_symlinked_directories()
    {
        var external = Path.Combine(_root, "external");
        var externalFile = Path.Combine(external, "keep.txt");
        Directory.CreateDirectory(external);
        File.WriteAllText(externalFile, "must survive");

        var tree = Path.Combine(_root, "node_modules");
        Directory.CreateDirectory(tree);

        // Force the read-only fallback path.
        var readOnly = Path.Combine(tree, "pkg.json");
        File.WriteAllText(readOnly, "{}");
        File.SetAttributes(readOnly, FileAttributes.ReadOnly);

        var link = Path.Combine(tree, "linked");
        try
        {
            Directory.CreateSymbolicLink(link, external);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return; // Symlink creation needs privilege/dev-mode; skip where unavailable.
        }

        _fs.DeleteDirectory(tree);

        Assert.False(Directory.Exists(tree));
        Assert.True(Directory.Exists(external));
        Assert.True(File.Exists(externalFile));
    }

    [Fact]
    public void EnumerateDirectories_keeps_hidden_folders_but_skips_links()
    {
        var hidden = Path.Combine(_root, ".venv");
        Directory.CreateDirectory(hidden);
        if (OperatingSystem.IsWindows())
        {
            File.SetAttributes(hidden, FileAttributes.Directory | FileAttributes.Hidden);
        }

        var target = Path.Combine(_root, "target-dir");
        Directory.CreateDirectory(target);
        if (!TryLink(Path.Combine(_root, "current"), target))
        {
            return;
        }

        var children = _fs.EnumerateDirectories(_root).Select(Path.GetFileName).ToList();

        Assert.Contains(".venv", children);
        Assert.Contains("target-dir", children);
        Assert.DoesNotContain("current", children);
        Assert.True(_fs.IsLink(Path.Combine(_root, "current")));
        Assert.Equal(Path.GetFullPath(target), _fs.ResolveLinkTarget(Path.Combine(_root, "current")));
        Assert.False(_fs.IsLink(target));
    }

    [Fact]
    public async Task A_folder_is_renamed_aside_then_deleted_leaving_nothing_behind()
    {
        var cache = Path.Combine(_root, "work", "cache");
        Directory.CreateDirectory(Path.Combine(cache, "sub"));
        File.WriteAllText(Path.Combine(cache, "sub", "a.bin"), "payload");

        var result = await new RealCleaner(cache).CleanAsync(RealContext());

        Assert.Equal(1, result.ItemsRemoved);
        Assert.False(Directory.Exists(cache));
        Assert.Empty(Directory.EnumerateFileSystemEntries(Path.Combine(_root, "work")));
    }

    [Fact]
    public async Task A_folder_with_an_open_file_is_left_complete_on_Windows()
    {
        if (!OperatingSystem.IsWindows())
        {
            return; // POSIX renames succeed with open files; the running-process check covers those.
        }

        var package = Path.Combine(_root, "work", "ffmpeg");
        Directory.CreateDirectory(Path.Combine(package, "bin"));
        Directory.CreateDirectory(Path.Combine(package, "doc"));
        File.WriteAllText(Path.Combine(package, "bin", "ffmpeg.exe"), "binary");
        File.WriteAllText(Path.Combine(package, "doc", "readme.txt"), "docs");

        CleanResult result;
        using (new FileStream(Path.Combine(package, "doc", "readme.txt"), FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            result = await new RealCleaner(package).CleanAsync(RealContext());
        }

        Assert.Equal(1, result.SkippedCount);
        Assert.False(result.HasErrors);
        Assert.True(File.Exists(Path.Combine(package, "bin", "ffmpeg.exe")));
        Assert.True(File.Exists(Path.Combine(package, "doc", "readme.txt")));
    }

    [Fact]
    public async Task The_project_search_does_not_follow_a_junction_into_another_tree()
    {
        var elsewhere = Path.Combine(_root, "elsewhere");
        Directory.CreateDirectory(Path.Combine(elsewhere, "obj"));
        File.WriteAllText(Path.Combine(elsewhere, "App.csproj"), "<Project />");
        File.WriteAllText(Path.Combine(elsewhere, "obj", "project.assets.json"), "{}");
        var workspace = Path.Combine(_root, "workspace");
        Directory.CreateDirectory(workspace);
        if (!TryLink(Path.Combine(workspace, "current"), elsewhere))
        {
            return;
        }

        var scan = await new BuildArtifactCleaner().ScanAsync(RealContext() with { ScanRoots = [workspace] });

        Assert.Empty(scan.Targets);
    }

    private CleanupContext RealContext()
    {
        var env = new FakeEnvironment
        {
            Os = OperatingSystem.IsWindows() ? OsPlatform.Windows : OperatingSystem.IsMacOS() ? OsPlatform.MacOs : OsPlatform.Linux,
            HomeDirectory = Path.Combine(_root, "home"),
            LocalAppDataDirectory = Path.Combine(_root, "home", "local"),
            AppDataDirectory = Path.Combine(_root, "home", "roaming"),
        };
        var runner = new FakeProcessRunner();
        return new CleanupContext
        {
            FileSystem = _fs,
            Environment = env,
            ProcessRunner = runner,
            Guard = DeletionGuard.Create(env, _fs, runner),
        };
    }

    /// <summary>
    /// Creates a junction on Windows (like scoop's <c>current</c>; needs no admin), else a symlink.
    /// Returns false where neither can be made.
    /// </summary>
    private static bool TryLink(string link, string target)
    {
        if (OperatingSystem.IsWindows())
        {
            var start = new System.Diagnostics.ProcessStartInfo("cmd.exe", $"/c mklink /J \"{link}\" \"{target}\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
            };
            using var mklink = System.Diagnostics.Process.Start(start)!;
            mklink.WaitForExit();
            return mklink.ExitCode == 0;
        }

        try
        {
            Directory.CreateSymbolicLink(link, target);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private sealed class RealCleaner(string target) : DirectoryCleanerBase
    {
        public override string Id => "real";

        public override string Name => "Real";

        public override string Category => "Test";

        protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) => [new CleanupPath(target)];
    }

    public void Dispose()
    {
        if (!Directory.Exists(_root))
        {
            return;
        }

        // Directory.Delete fails on read-only files.
        try
        {
            foreach (var file in Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories))
            {
                var attributes = File.GetAttributes(file);
                if ((attributes & FileAttributes.ReadOnly) != 0)
                {
                    File.SetAttributes(file, attributes & ~FileAttributes.ReadOnly);
                }
            }

            Directory.Delete(_root, recursive: true);
        }
        catch
        {
            // Leave the temp dir for the OS rather than fail teardown.
        }
    }
}

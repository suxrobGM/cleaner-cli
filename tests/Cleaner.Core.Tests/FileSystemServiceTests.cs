using Cleaner.Core.Services;
using Xunit;

namespace Cleaner.Core.Tests;

/// <summary>
/// Exercises the concrete <see cref="FileSystemService"/> against a real temp directory — the
/// read-only and reparse-point behaviour the in-memory fake can't model. Regression coverage for the
/// build-artifact sweep that hung when the read-only pre-walk followed symlinks/junctions.
/// </summary>
public sealed class FileSystemServiceTests : IDisposable
{
    private readonly FileSystemService _fs = new();
    private readonly string _root = Path.Combine(Path.GetTempPath(), "cleaner-fs-tests-" + Guid.NewGuid().ToString("N"));

    [Fact]
    public void DeleteDirectory_removes_tree_containing_read_only_files()
    {
        // NuGet and other package caches mark cached files read-only, which blocks a plain recursive
        // delete on Windows. The delete must still clear them and succeed.
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
        // A symlink/junction inside the tree (e.g. node_modules) must be removed as a link, never
        // followed — otherwise clearing read-only attributes or recursing walks the link's target
        // (a shared store, or a cycle) and the delete hangs.
        var external = Path.Combine(_root, "external");
        var externalFile = Path.Combine(external, "keep.txt");
        Directory.CreateDirectory(external);
        File.WriteAllText(externalFile, "must survive");

        var tree = Path.Combine(_root, "node_modules");
        Directory.CreateDirectory(tree);

        // Force the read-only fallback path (which is what walked the tree and followed links).
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

    public void Dispose()
    {
        if (!Directory.Exists(_root))
        {
            return;
        }

        // Best-effort cleanup; clear any read-only bits a failing test may have left behind.
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
            // Leave the temp dir for the OS to reap rather than failing teardown.
        }
    }
}

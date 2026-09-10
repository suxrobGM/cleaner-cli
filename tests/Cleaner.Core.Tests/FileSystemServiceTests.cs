using Cleaner.Core.Services;
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
        // Links must be removed without traversing their targets.
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

    public void Dispose()
    {
        if (!Directory.Exists(_root))
        {
            return;
        }

        // Clear read-only bits before best-effort teardown.
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

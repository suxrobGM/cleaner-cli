using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.Os;

/// <summary>Empties the recycle bin / trash for the current user.</summary>
public sealed class TrashCleaner : DirectoryCleanerBase
{
    public override string Id => "trash";

    public override string Name => "Recycle Bin / Trash";

    public override string Category => Categories.SystemCaches;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;

        if (env.IsWindows)
        {
            // Each user's bin is a SID-named folder only they can open; clearing inside the readable
            // ones empties ours without renaming folders Windows owns.
            foreach (var drive in env.FixedDriveRoots)
            {
                foreach (var userBin in context.FileSystem.EnumerateDirectories(Path.Combine(drive, "$Recycle.Bin")))
                {
                    yield return new CleanupPath(userBin, DeleteMode.ClearContents, drive);
                }
            }

            yield break;
        }

        if (env.IsMacOs)
        {
            yield return new CleanupPath(Path.Combine(env.HomeDirectory, ".Trash"), DeleteMode.ClearContents);
            yield break;
        }

        // Linux / freedesktop trash spec.
        var dataHome = env.GetEnvironmentVariable("XDG_DATA_HOME");
        var trashRoot = !string.IsNullOrWhiteSpace(dataHome)
            ? Path.Combine(dataHome, "Trash")
            : Path.Combine(env.HomeDirectory, ".local", "share", "Trash");
        yield return new CleanupPath(Path.Combine(trashRoot, "files"), DeleteMode.ClearContents, "files");
        yield return new CleanupPath(Path.Combine(trashRoot, "info"), DeleteMode.ClearContents, "info");
    }
}

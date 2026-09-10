using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.Os;

/// <summary>Empties the recycle bin / trash for the current user.</summary>
public sealed class TrashCleaner : DirectoryCleanerBase
{
    public override string Id => "trash";

    public override string Name => "Recycle Bin / Trash";

    public override string Category => Categories.OperatingSystem;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;

        if (env.IsWindows)
        {
            foreach (var drive in DriveInfo.GetDrives())
            {
                if (drive is { DriveType: DriveType.Fixed, IsReady: true })
                {
                    yield return new CleanupPath(
                        Path.Combine(drive.RootDirectory.FullName, "$Recycle.Bin"),
                        DeleteMode.ClearContents,
                        drive.Name);
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

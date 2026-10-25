using Cleaner.Core.Abstractions;
using Cleaner.Core.Utils;

namespace Cleaner.Core.Cleaners.Base;

/// <summary>
/// Removes approved targets one at a time. Each folder is re-checked against the guard, renamed aside
/// (atomic, and refused by Windows while anything inside is open), then deleted, so an interrupted
/// delete never leaves a half-removed install at its original path.
/// </summary>
internal sealed class TargetRemover(CleanupContext context, string cleanerId)
{
    /// <summary>Marker in the name of a folder that was renamed aside and is waiting to be deleted.</summary>
    public const string DeletingMarker = ".cleaner-deleting-";

    private readonly List<TargetOutcome> _outcomes = [];
    private readonly List<string> _errors = [];
    private long _bytesFreed;
    private int _removed;

    /// <summary>Lists renamed-aside folders whose delete didn't finish, so the next run can offer them again.</summary>
    public static string PendingDeletesFile(CleanupContext context) =>
        Path.Combine(context.Environment.HomeDirectory, ".cleaner", "pending-deletes.txt");

    public CleanResult Result => new(_bytesFreed, _removed, _errors) { Outcomes = _outcomes };

    public void Remove(CleanupTarget target, IProgress<CleanProgress>? progress, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        switch (target.Mode)
        {
            case DeleteMode.ToolManaged:
                return;
            case DeleteMode.ClearContents:
                ClearContents(target, progress, cancellationToken);
                return;
            case DeleteMode.DeleteFile:
                RemoveFile(target.Path, target.Description, progress);
                return;
            default:
                RemoveDirectory(target.Path, target.Description, progress);
                return;
        }
    }

    private void ClearContents(CleanupTarget target, IProgress<CleanProgress>? progress, CancellationToken cancellationToken)
    {
        var fs = context.FileSystem;
        if (!fs.DirectoryExists(target.Path))
        {
            Skip(target.Path, "no longer exists");
            return;
        }

        if (context.Guard.WhyProtected(target.Path, contentsOnly: true) is { } why)
        {
            Skip(target.Path, why, warn: true);
            return;
        }

        foreach (var entry in fs.EnumerateEntries(target.Path).ToList())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (fs.IsLink(entry))
            {
                Skip(entry, "is a link");
            }
            else if (fs.DirectoryExists(entry))
            {
                RemoveDirectory(entry, target.Description, progress);
            }
            else
            {
                RemoveFile(entry, target.Description, progress);
            }
        }
    }

    private void RemoveDirectory(string path, string? description, IProgress<CleanProgress>? progress)
    {
        var fs = context.FileSystem;
        if (!fs.DirectoryExists(path))
        {
            Skip(path, "no longer exists");
            return;
        }

        if (context.Guard.WhyProtected(path) is { } why)
        {
            Skip(path, why, warn: true);
            return;
        }

        if (context.Guard.WhyInUse(path) is { } busy)
        {
            Skip(path, busy);
            return;
        }

        var size = fs.GetDirectorySize(path);
        if (context.DryRun)
        {
            Removed(path, size, description, progress);
            return;
        }

        // A sibling keeps the rename on the same volume, which is what makes it atomic.
        var movedAside = Path.Combine(
            PathComparison.Parent(path) ?? path,
            PathComparison.Name(path) + DeletingMarker + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            fs.MoveDirectory(path, movedAside);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Nothing has been touched yet, so the folder stays complete where it was.
            Skip(path, $"is in use or locked, left intact ({ex.Message})");
            return;
        }

        try
        {
            fs.DeleteDirectory(movedAside);
            Removed(path, size, description, progress);
        }
        catch (Exception ex)
        {
            // The original path is already gone, so nothing looks installed but broken. Record the
            // rest so the next run can finish it.
            var left = fs.DirectoryExists(movedAside) ? fs.GetDirectorySize(movedAside) : 0;
            _bytesFreed += Math.Max(0, size - left);
            Fail(path, $"moved aside to {movedAside} but could not finish deleting it: {ex.Message}");
            TryRecordPending(movedAside);
        }
    }

    private void RemoveFile(string path, string? description, IProgress<CleanProgress>? progress)
    {
        var fs = context.FileSystem;
        if (!fs.FileExists(path))
        {
            Skip(path, "no longer exists");
            return;
        }

        if (context.Guard.WhyProtected(path) is { } why)
        {
            Skip(path, why, warn: true);
            return;
        }

        if (context.Guard.WhyInUse(path) is { } busy)
        {
            Skip(path, busy);
            return;
        }

        var size = fs.GetFileSize(path);
        if (context.DryRun)
        {
            Removed(path, size, description, progress);
            return;
        }

        try
        {
            fs.DeleteFile(path);
            Removed(path, size, description, progress);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A file another program holds open is expected in temp folders; leave it for next time.
            Skip(path, $"is in use or locked ({ex.Message})");
        }
        catch (Exception ex)
        {
            Fail(path, ex.Message);
        }
    }

    private void Removed(string path, long size, string? description, IProgress<CleanProgress>? progress)
    {
        _bytesFreed += size;
        _removed++;
        _outcomes.Add(new TargetOutcome(path, OutcomeKind.Removed, size));
        context.Logger?.Detail($"[{cleanerId}] {(context.DryRun ? "would remove" : "removed")} {path} ({SizeFormatter.Humanize(size)})");
        progress?.Report(new CleanProgress(description ?? path, size));
    }

    private void Skip(string path, string reason, bool warn = false)
    {
        _outcomes.Add(new TargetOutcome(path, OutcomeKind.Skipped, Reason: reason));
        var message = $"[{cleanerId}] skipped {path}: {reason}";
        if (warn)
        {
            context.Logger?.Warn(message);
        }
        else
        {
            context.Logger?.Detail(message);
        }
    }

    private void Fail(string path, string reason)
    {
        _outcomes.Add(new TargetOutcome(path, OutcomeKind.Failed, Reason: reason));
        _errors.Add($"{path}: {reason}");
        context.Logger?.Error($"[{cleanerId}] failed {path}: {reason}");
    }

    private void TryRecordPending(string movedAside)
    {
        try
        {
            context.FileSystem.AppendLine(PendingDeletesFile(context), movedAside);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            context.Logger?.Warn($"[{cleanerId}] could not record {movedAside} for a later retry: {ex.Message}");
        }
    }
}

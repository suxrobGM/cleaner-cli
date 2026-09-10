using Cleaner.Core.Abstractions;

namespace Cleaner.Core.Cleaners.Base;

/// <summary>
/// Base class for the common cleaner shape: declare a set of cache directories and let the base
/// handle existence checks, size measurement, dry-run accounting, deletion, and error capture.
/// Subclasses implement <see cref="GetTargets"/> (and usually only that).
/// </summary>
public abstract class DirectoryCleanerBase : ICleaner
{
    public abstract string Id { get; }

    public abstract string Name { get; }

    public abstract string Category { get; }

    public virtual bool RequiresElevation => false;

    /// <inheritdoc cref="ICleaner.SupportsSizeEstimate"/>
    public virtual bool SupportsSizeEstimate => true;

    /// <inheritdoc cref="ICleaner.ConfirmationWarning"/>
    public virtual string? ConfirmationWarning => null;

    /// <summary>Candidate targets, which may not exist and may be files (<see cref="DeleteMode.DeleteFile"/>).</summary>
    protected abstract IEnumerable<CleanupPath> GetTargets(CleanupContext context);

    public virtual bool IsApplicable(CleanupContext context) => true;

    public virtual bool IsAvailable(CleanupContext context) =>
        ExistingTargets(context).Any();

    public virtual Task<ScanResult> ScanAsync(CleanupContext context, CancellationToken cancellationToken = default)
    {
        var targets = new List<CleanupTarget>();
        foreach (var path in ExistingTargets(context))
        {
            cancellationToken.ThrowIfCancellationRequested();
            targets.Add(new CleanupTarget(path.Path, SizeOf(context, path), path.Description));
        }

        return Task.FromResult(new ScanResult(targets));
    }

    public virtual Task<CleanResult> CleanAsync(
        CleanupContext context,
        IProgress<CleanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        long bytesFreed = 0;
        var itemsRemoved = 0;
        var errors = new List<string>();

        foreach (var path in ExistingTargets(context))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var size = SizeOf(context, path);

            try
            {
                if (!context.DryRun)
                {
                    Remove(context, path);
                }

                bytesFreed += size;
                itemsRemoved++;
                progress?.Report(new CleanProgress(path.Description ?? path.Path, size));
            }
            catch (Exception ex)
            {
                errors.Add($"{path.Path}: {ex.Message}");
            }
        }

        return Task.FromResult(new CleanResult(bytesFreed, itemsRemoved, errors));
    }

    private static void Remove(CleanupContext context, CleanupPath path)
    {
        switch (path.Mode)
        {
            case DeleteMode.ClearContents:
                context.FileSystem.DeleteContents(path.Path);
                break;
            case DeleteMode.DeleteFile:
                context.FileSystem.DeleteFile(path.Path);
                break;
            default:
                context.FileSystem.DeleteDirectory(path.Path);
                break;
        }
    }

    private static long SizeOf(CleanupContext context, CleanupPath path) =>
        path.Mode == DeleteMode.DeleteFile
            ? context.FileSystem.GetFileSize(path.Path)
            : context.FileSystem.GetDirectorySize(path.Path);

    private static bool Exists(CleanupContext context, CleanupPath path) =>
        path.Mode == DeleteMode.DeleteFile
            ? context.FileSystem.FileExists(path.Path)
            : context.FileSystem.DirectoryExists(path.Path);

    /// <summary>Targets that actually exist on disk (file or directory), de-duplicated by path.</summary>
    protected IEnumerable<CleanupPath> ExistingTargets(CleanupContext context)
    {
        // Linux paths are case-sensitive; Windows and (default) macOS volumes are not.
        var comparer = context.Environment.IsLinux ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        var seen = new HashSet<string>(comparer);
        foreach (var path in GetTargets(context))
        {
            if (string.IsNullOrWhiteSpace(path.Path))
            {
                continue;
            }

            // Normalize separators so the same directory spelled two ways de-dupes, and check
            // existence first so a nonexistent candidate doesn't shadow a real one it aliases.
            var key = path.Path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            if (Exists(context, path) && seen.Add(key))
            {
                yield return path;
            }
        }
    }
}

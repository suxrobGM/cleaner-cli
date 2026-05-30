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

    /// <summary>The candidate directories this cleaner targets. May include paths that don't exist.</summary>
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
            var size = context.FileSystem.GetDirectorySize(path.Path);
            targets.Add(new CleanupTarget(path.Path, size, path.Description));
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
            var size = context.FileSystem.GetDirectorySize(path.Path);

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
            default:
                context.FileSystem.DeleteDirectory(path.Path);
                break;
        }
    }

    /// <summary>Targets that actually exist on disk, de-duplicated by path.</summary>
    protected IEnumerable<CleanupPath> ExistingTargets(CleanupContext context)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in GetTargets(context))
        {
            if (string.IsNullOrWhiteSpace(path.Path))
            {
                continue;
            }

            if (seen.Add(path.Path) && context.FileSystem.DirectoryExists(path.Path))
            {
                yield return path;
            }
        }
    }
}

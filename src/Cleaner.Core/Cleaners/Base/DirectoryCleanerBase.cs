using Cleaner.Core.Abstractions;

namespace Cleaner.Core.Cleaners.Base;

/// <summary>
/// The common cleaner shape: declare the cache directories, and the base handles existence checks,
/// sizing, dry-run accounting, deletion, and error capture. Subclasses usually override only
/// <see cref="GetTargets"/>, or <see cref="GetTargetsAsync"/> when discovery itself needs I/O.
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
    protected virtual IEnumerable<CleanupPath> GetTargets(CleanupContext context) => [];

    /// <summary>
    /// Candidate targets when discovery needs I/O of its own — a registry read, an external command.
    /// Defaults to <see cref="GetTargets"/>. A subclass overriding this instead should also override
    /// <see cref="IsAvailable"/>, which stays synchronous.
    /// </summary>
    protected virtual ValueTask<IEnumerable<CleanupPath>> GetTargetsAsync(
        CleanupContext context,
        CancellationToken cancellationToken) => new(GetTargets(context));

    public virtual bool IsApplicable(CleanupContext context) => true;

    public virtual bool IsAvailable(CleanupContext context) =>
        ExistingTargets(context).Any();

    public virtual async Task<ScanResult> ScanAsync(CleanupContext context, CancellationToken cancellationToken = default)
    {
        var targets = new List<CleanupTarget>();
        foreach (var path in await ExistingTargetsAsync(context, cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            targets.Add(new CleanupTarget(path.Path, SizeOf(context, path), path.Description));
        }

        return new ScanResult(targets);
    }

    public virtual async Task<CleanResult> CleanAsync(
        CleanupContext context,
        IProgress<CleanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        long bytesFreed = 0;
        var itemsRemoved = 0;
        var errors = new List<string>();

        foreach (var path in await ExistingTargetsAsync(context, cancellationToken).ConfigureAwait(false))
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

        return new CleanResult(bytesFreed, itemsRemoved, errors);
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

    /// <summary>On-disk size of a target, measured as whatever its <see cref="DeleteMode"/> says it is.</summary>
    protected static long SizeOf(CleanupContext context, CleanupPath path) =>
        path.Mode == DeleteMode.DeleteFile
            ? context.FileSystem.GetFileSize(path.Path)
            : context.FileSystem.GetDirectorySize(path.Path);

    private static bool Exists(CleanupContext context, CleanupPath path) =>
        path.Mode == DeleteMode.DeleteFile
            ? context.FileSystem.FileExists(path.Path)
            : context.FileSystem.DirectoryExists(path.Path);

    /// <summary>Targets that actually exist on disk (file or directory), de-duplicated by path.</summary>
    protected IEnumerable<CleanupPath> ExistingTargets(CleanupContext context) =>
        Existing(context, GetTargets(context));

    /// <inheritdoc cref="ExistingTargets"/>
    protected async ValueTask<IReadOnlyList<CleanupPath>> ExistingTargetsAsync(
        CleanupContext context,
        CancellationToken cancellationToken)
    {
        var candidates = await GetTargetsAsync(context, cancellationToken).ConfigureAwait(false);
        return [.. Existing(context, candidates)];
    }

    private static IEnumerable<CleanupPath> Existing(CleanupContext context, IEnumerable<CleanupPath> candidates)
    {
        // Linux paths are case-sensitive; Windows and (default) macOS volumes are not.
        var comparer = context.Environment.IsLinux ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
        var seen = new HashSet<string>(comparer);
        foreach (var path in candidates)
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

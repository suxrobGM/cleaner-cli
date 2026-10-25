using Cleaner.Core.Abstractions;
using Cleaner.Core.Utils;

namespace Cleaner.Core.Cleaners.Base;

/// <summary>
/// Base implementation for cleaners that declare filesystem targets. Handles sizing, dry runs,
/// deletion, and error capture; subclasses usually override <see cref="GetTargets"/>.
/// </summary>
public abstract class DirectoryCleanerBase : ICleaner
{
    public abstract string Id { get; }

    public abstract string Name { get; }

    public abstract string Category { get; }

    public virtual bool RequiresElevation => false;

    /// <inheritdoc cref="ICleaner.SupportsSizeEstimate"/>
    public virtual bool SupportsSizeEstimate => true;

    /// <inheritdoc cref="ICleaner.SupportsPartialSelection"/>
    /// <remarks>
    /// True because <see cref="CleanAsync"/> deletes exactly the approved targets <see cref="ScanAsync"/> reported.
    /// An override that reaches past those targets must say so by returning false.
    /// </remarks>
    public virtual bool SupportsPartialSelection => true;

    /// <inheritdoc cref="ICleaner.ConfirmationWarning"/>
    public virtual string? ConfirmationWarning => null;

    /// <inheritdoc cref="ICleaner.OptIn"/>
    public virtual bool OptIn => false;

    /// <summary>Candidate targets, which may not exist and may be files (<see cref="DeleteMode.DeleteFile"/>).</summary>
    protected virtual IEnumerable<CleanupPath> GetTargets(CleanupContext context) => [];

    /// <summary>
    /// Candidate targets when discovery requires I/O. Defaults to <see cref="GetTargets"/>;
    /// asynchronous implementations should also override synchronous <see cref="IsAvailable"/>.
    /// </summary>
    protected virtual ValueTask<IEnumerable<CleanupPath>> GetTargetsAsync(
        CleanupContext context,
        CancellationToken cancellationToken) => new(GetTargets(context));

    public virtual bool IsApplicable(CleanupContext context) => true;

    /// <summary>Some target exists that the guard would let this cleaner remove.</summary>
    public virtual bool IsAvailable(CleanupContext context) =>
        ExistingTargets(context).Any(path =>
            path.Mode == DeleteMode.ToolManaged
            || context.Guard.WhyProtected(path.Path, contentsOnly: path.Mode == DeleteMode.ClearContents) is null);

    public virtual async Task<ScanResult> ScanAsync(CleanupContext context, CancellationToken cancellationToken = default)
    {
        var targets = new List<CleanupTarget>();
        var protectedPaths = 0;
        foreach (var path in await ExistingTargetsAsync(context, cancellationToken).ConfigureAwait(false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (bytes, blocked) = Measure(context, path);
            protectedPaths += blocked;
            if (bytes is { } size)
            {
                targets.Add(new CleanupTarget(path.Path, size, path.Mode, path.Description));
            }
        }

        var notes = ScanNotes(context).ToList();
        if (protectedPaths > 0)
        {
            notes.Add($"{protectedPaths} protected path(s) left alone; the log says why");
        }

        return new ScanResult(targets) { Notes = notes };
    }

    /// <summary>Anything about discovery the user should see, such as a scan root that was refused.</summary>
    protected virtual IEnumerable<string> ScanNotes(CleanupContext context) => [];

    /// <summary>
    /// Removes exactly the approved targets. Each is checked against the guard again first, and
    /// nothing is rediscovered, so the run can't reach past what the preview showed.
    /// </summary>
    public virtual Task<CleanResult> CleanAsync(
        CleanupContext context,
        IReadOnlyList<CleanupTarget> approved,
        IProgress<CleanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var remover = new TargetRemover(context, Id);
        foreach (var target in approved)
        {
            remover.Remove(target, progress, cancellationToken);
        }

        return Task.FromResult(remover.Result);
    }

    /// <summary>
    /// What a target would free once the guard has had its say: null when it is protected outright,
    /// and for <see cref="DeleteMode.ClearContents"/> the sum of the children that may go.
    /// </summary>
    private (long? Bytes, int Blocked) Measure(CleanupContext context, CleanupPath path)
    {
        var guard = context.Guard;
        if (path.Mode == DeleteMode.ToolManaged)
        {
            return (SizeOf(context, path), 0);
        }

        if (guard.WhyProtected(path.Path, contentsOnly: path.Mode == DeleteMode.ClearContents) is { } why)
        {
            LogProtected(context, path.Path, why);
            return (null, 1);
        }

        if (path.Mode != DeleteMode.ClearContents)
        {
            return (SizeOf(context, path), 0);
        }

        long total = 0;
        var blocked = 0;
        foreach (var entry in context.FileSystem.EnumerateEntries(path.Path))
        {
            if (context.FileSystem.IsLink(entry))
            {
                continue;
            }

            if (guard.WhyProtected(entry) is { } childWhy)
            {
                LogProtected(context, entry, childWhy);
                blocked++;
                continue;
            }

            total += context.FileSystem.DirectoryExists(entry)
                ? context.FileSystem.GetDirectorySize(entry)
                : context.FileSystem.GetFileSize(entry);
        }

        return (total, blocked);
    }

    private void LogProtected(CleanupContext context, string path, string why) =>
        context.Logger?.Detail($"[{Id}] not offered: {path} {why}");

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
        var seen = new HashSet<string>(PathComparison.ComparerFor(context.Environment.IsLinux));
        foreach (var path in candidates)
        {
            if (string.IsNullOrWhiteSpace(path.Path))
            {
                continue;
            }

            // Normalize separators so the same directory spelled two ways de-dupes, and check
            // existence first so a nonexistent candidate doesn't shadow a real one it aliases.
            var key = PathComparison.Normalize(path.Path);
            if (Exists(context, path) && seen.Add(key))
            {
                yield return path;
            }
        }
    }
}

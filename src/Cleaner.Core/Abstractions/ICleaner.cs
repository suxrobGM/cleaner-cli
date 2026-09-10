namespace Cleaner.Core.Abstractions;

/// <summary>
/// Cleaner's extension point: every cache target is one of these. Add one by deriving from a base
/// class and registering it once in the composition root.
/// </summary>
public interface ICleaner
{
    /// <summary>Stable, kebab-case identifier (e.g. "nuget", "npm").</summary>
    string Id { get; }

    /// <summary>Human-friendly name shown in lists and prompts (e.g. "NuGet package cache").</summary>
    string Name { get; }

    /// <summary>Grouping for display and bulk selection (e.g. "Package managers").</summary>
    string Category { get; }

    /// <summary>True if removing this cleaner's targets requires administrator/root privileges.</summary>
    bool RequiresElevation { get; }

    /// <summary>
    /// False when nothing on disk can be measured directly, so a size may only be knowable after the
    /// cleaner runs. Such a cleaner can still estimate — several ask their own tool — and the UI
    /// shows the number when there is one, labelling the row instead of showing 0 B when there isn't.
    /// </summary>
    bool SupportsSizeEstimate => true;

    /// <summary>
    /// States the trade-off for cleaners that cost more than a re-fetch (deleting Windows.old gives
    /// up upgrade rollback). Non-null earns its own yes/no before the run-wide confirmation.
    /// </summary>
    string? ConfirmationWarning => null;

    /// <summary>True if this cleaner is meaningful on the current operating system.</summary>
    bool IsApplicable(CleanupContext context);

    /// <summary>True if the tool/paths this cleaner targets are actually present on this machine.</summary>
    bool IsAvailable(CleanupContext context);

    /// <summary>Measure what would be removed without deleting anything.</summary>
    Task<ScanResult> ScanAsync(CleanupContext context, CancellationToken cancellationToken = default);

    /// <summary>Remove the targets (honoring <see cref="CleanupContext.DryRun"/>), reporting progress.</summary>
    Task<CleanResult> CleanAsync(
        CleanupContext context,
        IProgress<CleanProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

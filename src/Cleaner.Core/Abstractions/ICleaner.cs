namespace Cleaner.Core.Abstractions;

/// <summary>A cleanup target, registered explicitly in the composition root.</summary>
public interface ICleaner
{
    /// <summary>Stable, kebab-case identifier (e.g. "nuget", "npm").</summary>
    string Id { get; }

    string Name { get; }

    /// <summary>Grouping for display and bulk selection (e.g. "Package managers").</summary>
    string Category { get; }

    bool RequiresElevation { get; }

    /// <summary>False when the cleaner may not know its size before running.</summary>
    bool SupportsSizeEstimate => true;

    /// <summary>
    /// False when cleanup hands the whole job to a tool that can't be told which targets to keep.
    /// </summary>
    bool SupportsPartialSelection => true;

    /// <summary>Warning shown before a cleanup with a real trade-off.</summary>
    string? ConfirmationWarning => null;

    /// <summary>
    /// Left out of "All cleaners" and must be picked by name, because its reach depends on where
    /// Cleaner was started.
    /// </summary>
    bool OptIn => false;

    /// <summary>True if this cleaner is meaningful on the current operating system.</summary>
    bool IsApplicable(CleanupContext context);

    /// <summary>True if the tool or paths this cleaner targets are present on this machine.</summary>
    bool IsAvailable(CleanupContext context);

    Task<ScanResult> ScanAsync(CleanupContext context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes only the <paramref name="approved"/> targets the user reviewed, never anything found
    /// since (honoring <see cref="CleanupContext.DryRun"/>).
    /// </summary>
    Task<CleanResult> CleanAsync(
        CleanupContext context,
        IReadOnlyList<CleanupTarget> approved,
        IProgress<CleanProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

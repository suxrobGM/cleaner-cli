namespace Cleaner.Core.Abstractions;

/// <summary>
/// The extension point of Cleaner. Every cache target — dev tool, OS area, or application — is an
/// <see cref="ICleaner"/>. Add a new one by implementing this (usually via a base class) and
/// registering it once in the composition root.
/// </summary>
public interface ICleaner
{
    /// <summary>Stable, kebab-case identifier used on the command line (e.g. "nuget", "npm").</summary>
    string Id { get; }

    /// <summary>Human-friendly name shown in lists and prompts (e.g. "NuGet package cache").</summary>
    string Name { get; }

    /// <summary>Grouping for display and bulk selection (e.g. "Package managers").</summary>
    string Category { get; }

    /// <summary>True if removing this cleaner's targets requires administrator/root privileges.</summary>
    bool RequiresElevation { get; }

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

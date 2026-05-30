namespace Cleaner.Core.Services;

/// <summary>
/// Orchestrates self-update: reports the running version, checks GitHub for a newer release, and
/// (on request) downloads it, swaps the running binary, and relaunches. Networking and JSON live in
/// <see cref="IGitHubReleaseClient"/>; this service is the policy on top.
/// </summary>
public interface IUpdateService
{
    /// <summary>The version of the currently running binary (e.g. <c>1.0.0</c>).</summary>
    string CurrentVersion { get; }

    /// <summary>Ask GitHub for the latest release and compare it against <see cref="CurrentVersion"/>.</summary>
    Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Download the asset from <paramref name="check"/>, replace the running executable, and relaunch
    /// it. Throws <see cref="InvalidOperationException"/> with an actionable message on failure (no
    /// matching asset, insufficient permissions, etc.).
    /// </summary>
    Task ApplyAsync(UpdateCheckResult check, IProgress<double>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Best-effort removal of the <c>*.old</c> backup left next to the running binary by a previous
    /// update on Windows. Safe to call on every startup; errors are swallowed.
    /// </summary>
    void CleanupStaleBackup();
}

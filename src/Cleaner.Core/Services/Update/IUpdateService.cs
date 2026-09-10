namespace Cleaner.Core.Services;

/// <summary>
/// Self-update policy: report the running version, check GitHub, and on request download, swap the
/// binary, and relaunch. Networking and JSON live in <see cref="IGitHubReleaseClient"/>.
/// </summary>
public interface IUpdateService
{
    /// <summary>The version of the currently running binary (e.g. <c>1.0.0</c>).</summary>
    string CurrentVersion { get; }

    /// <summary>Ask GitHub for the latest release and compare it against <see cref="CurrentVersion"/>.</summary>
    Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Download the asset, replace the running executable, and relaunch. Throws
    /// <see cref="InvalidOperationException"/> with an actionable message on failure.
    /// </summary>
    Task ApplyAsync(UpdateCheckResult check, IProgress<double>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove the <c>*.old</c> backup a previous Windows update left beside the binary. Best-effort,
    /// safe on every startup.
    /// </summary>
    void CleanupStaleBackup();
}

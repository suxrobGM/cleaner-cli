namespace Cleaner.Core.Services;

/// <summary>Coordinates version checks and self-updates through <see cref="IGitHubReleaseClient"/>.</summary>
public interface IUpdateService
{
    /// <summary>The version of the currently running binary (e.g. <c>1.0.0</c>).</summary>
    string CurrentVersion { get; }

    /// <summary>Ask GitHub for the latest release and compare it against <see cref="CurrentVersion"/>.</summary>
    Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads, replaces, and relaunches; throws <see cref="InvalidOperationException"/> with an actionable message.
    /// </summary>
    Task ApplyAsync(UpdateCheckResult check, IProgress<double>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>Best-effort removal of a stale Windows update backup.</summary>
    void CleanupStaleBackup();
}

namespace Cleaner.Core.Services;

/// <summary>Checks for and applies self-updates from GitHub releases.</summary>
public interface IUpdateService
{
    /// <summary>The running binary's version, e.g. <c>1.0.0</c>.</summary>
    string CurrentVersion { get; }

    Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads, replaces, and relaunches the binary. Failures throw <see cref="InvalidOperationException"/>
    /// with a message the user can act on.
    /// </summary>
    Task ApplyAsync(UpdateCheckResult check, IProgress<double>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>Best-effort removal of the old binary a Windows update leaves behind.</summary>
    void CleanupStaleBackup();
}

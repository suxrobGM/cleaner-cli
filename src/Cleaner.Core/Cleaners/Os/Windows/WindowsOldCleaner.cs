using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Services;

namespace Cleaner.Core.Cleaners.Os;

/// <summary>
/// The previous Windows installation left by an upgrade. Deleting it removes rollback capability;
/// files are owned by TrustedInstaller, so ownership is taken on this directory before deletion.
/// </summary>
public sealed class WindowsOldCleaner : WindowsCleanerBase
{
    public override string Id => "windows-old";

    public override string Name => "Windows.old (previous installation)";

    public override bool RequiresElevation => true;

    public override string ConfirmationWarning =>
        "deleting this removes the ability to roll back to the previous Windows version, and Windows " +
        "normally clears it automatically about 10 days after an upgrade";

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var windows = context.Environment.WindowsDirectory;
        if (windows is not null)
        {
            yield return new CleanupPath(OsPaths.FromWindowsDriveRoot(windows, "Windows.old"));
        }
    }

    public override async Task<CleanResult> CleanAsync(
        CleanupContext context,
        IReadOnlyList<CleanupTarget> approved,
        IProgress<CleanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!context.DryRun)
        {
            // TrustedInstaller/SYSTEM own most of the tree; a plain recursive delete gets access
            // denied. Take ownership and grant Administrators full control — on approved, unprotected
            // paths only, since the grant itself can't be undone.
            foreach (var target in approved.Where(t => context.Guard.WhyProtected(t.Path) is null))
            {
                await context.ProcessRunner
                    .RunAsync("takeown", ["/F", target.Path, "/R", "/A", "/D", "Y"], cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
                await context.ProcessRunner
                    .RunAsync("icacls", [target.Path, "/grant", "Administrators:F", "/T", "/C"], cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        return await base.CleanAsync(context, approved, progress, cancellationToken).ConfigureAwait(false);
    }
}

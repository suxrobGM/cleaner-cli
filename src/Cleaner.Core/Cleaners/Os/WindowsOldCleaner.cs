using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Services;

namespace Cleaner.Core.Cleaners.Os;

/// <summary>
/// The previous Windows installation left behind by an upgrade. Scans always report its size, but
/// deleting it removes the ability to roll back the upgrade, so cleaning requires <c>--force</c>.
/// Files are owned by TrustedInstaller, so ownership is taken (on this directory only) before deletion.
/// </summary>
public sealed class WindowsOldCleaner : WindowsCleanerBase
{
    public override string Id => "windows-old";

    public override string Name => "Windows.old (previous installation)";

    public override bool RequiresElevation => true;

    public override bool RequiresForce => true;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var windows = context.Environment.WindowsDirectory;
        if (windows is not null)
        {
            var root = Path.GetPathRoot(windows) ?? @"C:\";
            yield return new CleanupPath(Path.Combine(root, "Windows.old"));
        }
    }

    public override async Task<CleanResult> CleanAsync(
        CleanupContext context,
        IProgress<CleanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!context.DryRun)
        {
            // TrustedInstaller/SYSTEM own most of the tree; a plain recursive delete gets access
            // denied. Take ownership and grant Administrators full control — on this path only.
            foreach (var path in ExistingTargets(context))
            {
                await context.ProcessRunner
                    .RunAsync("takeown", ["/F", path.Path, "/R", "/A", "/D", "Y"], cancellationToken)
                    .ConfigureAwait(false);
                await context.ProcessRunner
                    .RunAsync("icacls", [path.Path, "/grant", "Administrators:F", "/T", "/C"], cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        return await base.CleanAsync(context, progress, cancellationToken).ConfigureAwait(false);
    }
}

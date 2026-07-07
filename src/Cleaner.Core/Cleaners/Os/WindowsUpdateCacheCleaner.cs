using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Services;

namespace Cleaner.Core.Cleaners.Os;

/// <summary>Downloaded Windows Update payloads (SoftwareDistribution\Download). Needs elevation.</summary>
public sealed class WindowsUpdateCacheCleaner : WindowsCleanerBase
{
    public override string Id => "windows-update";

    public override string Name => "Windows Update cache";

    public override bool RequiresElevation => true;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var windows = context.Environment.WindowsDirectory;
        if (windows is not null)
        {
            yield return new CleanupPath(Path.Combine(windows, "SoftwareDistribution", "Download"), DeleteMode.ClearContents);
        }
    }
}

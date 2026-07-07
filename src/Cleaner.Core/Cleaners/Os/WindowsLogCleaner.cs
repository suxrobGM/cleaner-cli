using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Services;

namespace Cleaner.Core.Cleaners.Os;

/// <summary>Windows servicing and component logs (CBS, DISM, WindowsUpdate). Needs elevation.</summary>
public sealed class WindowsLogCleaner : WindowsCleanerBase
{
    public override string Id => "windows-logs";

    public override string Name => "Windows system logs";

    public override bool RequiresElevation => true;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var windows = context.Environment.WindowsDirectory;
        if (windows is not null)
        {
            yield return new CleanupPath(Path.Combine(windows, "Logs"), DeleteMode.ClearContents);
        }
    }
}

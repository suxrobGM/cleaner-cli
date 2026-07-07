using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Services;

namespace Cleaner.Core.Cleaners.Os;

/// <summary>The machine-wide Windows\Temp directory. Needs elevation.</summary>
public sealed class WindowsTempCleaner : WindowsCleanerBase
{
    public override string Id => "windows-temp";

    public override string Name => "Windows temp (system)";

    public override bool RequiresElevation => true;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var windows = context.Environment.WindowsDirectory;
        if (windows is not null)
        {
            yield return new CleanupPath(Path.Combine(windows, "Temp"), DeleteMode.ClearContents);
        }
    }
}

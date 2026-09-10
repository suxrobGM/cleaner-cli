using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.Os;

/// <summary>
/// <c>C:\$WinREAgent</c>, the scratch folder Windows Setup uses during a feature update. It is meant
/// to be removed once the update succeeds but is routinely left behind.
/// </summary>
public sealed class WinReAgentCleaner : WindowsCleanerBase
{
    public override string Id => "winre-agent";

    public override string Name => "Windows Update scratch ($WinREAgent)";

    public override bool RequiresElevation => true;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var windows = context.Environment.WindowsDirectory;
        if (windows is not null)
        {
            yield return new CleanupPath(OsPaths.FromWindowsDriveRoot(windows, "$WinREAgent"));
        }
    }
}

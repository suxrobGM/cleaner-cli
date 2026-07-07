using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Services;

namespace Cleaner.Core.Cleaners.Os;

/// <summary>Leftover downloaded installers and ActiveX/Java applet caches. Needs elevation.</summary>
public sealed class DownloadedProgramFilesCleaner : WindowsCleanerBase
{
    public override string Id => "downloaded-program-files";

    public override string Name => "Downloaded program files";

    public override bool RequiresElevation => true;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var windows = context.Environment.WindowsDirectory;
        if (windows is not null)
        {
            yield return new CleanupPath(Path.Combine(windows, "Downloaded Program Files"), DeleteMode.ClearContents, "ActiveX/Java applets");
            yield return new CleanupPath(Path.Combine(windows, "Downloaded Installations"), DeleteMode.ClearContents, "installers");
        }
    }
}

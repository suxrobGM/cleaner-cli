using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Services;

namespace Cleaner.Core.Cleaners.Os;

/// <summary>
/// winget installer downloads and diagnostic logs. The source index under LocalCache is left alone —
/// purging it breaks winget sources until a manual refresh.
/// </summary>
public sealed class WingetCleaner : WindowsCleanerBase
{
    public override string Id => "winget";

    public override string Name => "winget downloads & logs";

    public override string Category => Categories.SystemPackageManagers;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return new CleanupPath(Path.Combine(env.TempDirectory, "WinGet"), DeleteMode.ClearContents, "installer downloads");
        yield return new CleanupPath(
            Path.Combine(env.LocalAppDataDirectory, "Packages",
                "Microsoft.DesktopAppInstaller_8wekyb3d8bbwe", "LocalState", "DiagOutputDir"),
            DeleteMode.ClearContents,
            "logs");
    }
}

using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.Applications;

/// <summary>OneDrive client logs and setup logs; synced content is never touched.</summary>
public sealed class OneDriveCleaner : DirectoryCleanerBase
{
    public override string Id => "onedrive";

    public override string Name => "OneDrive logs";

    public override string Category => Categories.Applications;

    public override bool IsApplicable(CleanupContext context) => context.Environment.IsWindows;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var root = Path.Combine(context.Environment.LocalAppDataDirectory, "Microsoft", "OneDrive");
        yield return new CleanupPath(Path.Combine(root, "logs"), DeleteMode.ClearContents, "client logs");
        yield return new CleanupPath(Path.Combine(root, "setup", "logs"), DeleteMode.ClearContents, "setup logs");
    }
}

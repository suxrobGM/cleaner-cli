using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>asdf download and temp directories; installed tools under installs/ are kept.</summary>
public sealed class AsdfCleaner : DirectoryCleanerBase
{
    public override string Id => "asdf";

    public override string Name => "asdf downloads";

    public override string Category => Categories.ToolingDownloads;

    public override bool IsApplicable(CleanupContext context) => !context.Environment.IsWindows;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        var root = OsPaths.Env(env, "ASDF_DATA_DIR") ?? env.HomePath(".asdf");
        yield return new CleanupPath(Path.Combine(root, "downloads"), DeleteMode.ClearContents, "downloads");
        yield return new CleanupPath(Path.Combine(root, "tmp"), DeleteMode.ClearContents, "temp");
    }
}

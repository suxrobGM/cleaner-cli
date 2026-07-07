using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.Applications;

/// <summary>Adobe shared media caches (Premiere/After Effects render and peak files; regenerated).</summary>
public sealed class AdobeMediaCacheCleaner : DirectoryCleanerBase
{
    public override string Id => "adobe-media-cache";

    public override string Name => "Adobe media cache";

    public override string Category => Categories.Applications;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        var common = env.IsWindows
            ? Path.Combine(env.AppDataDirectory, "Adobe", "Common")
            : Path.Combine(env.HomeDirectory, "Library", "Application Support", "Adobe", "Common");

        yield return new CleanupPath(Path.Combine(common, "Media Cache Files"), DeleteMode.ClearContents, "media cache files");
        yield return new CleanupPath(Path.Combine(common, "Media Cache"), DeleteMode.ClearContents, "media cache database");
        yield return new CleanupPath(Path.Combine(common, "Peak Files"), DeleteMode.ClearContents, "audio peak files");
    }
}

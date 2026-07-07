using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>PHP Composer download cache.</summary>
public sealed class ComposerCleaner : DirectoryCleanerBase
{
    public override string Id => "composer";

    public override string Name => "Composer cache";

    public override string Category => Categories.Languages;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return env.IsWindows
            ? new CleanupPath(Path.Combine(env.LocalAppDataDirectory, "Composer"), DeleteMode.ClearContents)
            : new CleanupPath(Path.Combine(env.CacheDirectory, "composer"), DeleteMode.ClearContents);
    }
}

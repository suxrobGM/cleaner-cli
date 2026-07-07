using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>Expo caches: the global <c>~/.expo</c> store and the project-local <c>.expo</c> folder.</summary>
public sealed class ExpoCleaner : DirectoryCleanerBase
{
    public override string Id => "expo";

    public override string Name => "Expo caches";

    public override string Category => Categories.Mobile;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        yield return new CleanupPath(context.Environment.HomePath(".expo"), DeleteMode.ClearContents, "global cache");
        yield return new CleanupPath(Path.Combine(context.WorkingDirectory, ".expo"), Description: "project cache");
    }
}

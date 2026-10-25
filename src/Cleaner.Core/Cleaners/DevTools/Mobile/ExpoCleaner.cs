using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// The project-local Expo <c>.expo</c> cache. The global <c>~/.expo</c> is kept: it holds the Expo
/// login state.
/// </summary>
public sealed class ExpoCleaner : DirectoryCleanerBase
{
    public override string Id => "expo";

    public override string Name => "Expo caches";

    public override string Category => Categories.Mobile;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        ProjectRoots.InWorkingDirectory(context, ".expo") is { } expo ? [new CleanupPath(expo, Description: "project cache")] : [];
}

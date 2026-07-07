using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>pipenv virtualenv/cache directory.</summary>
public sealed class PipenvCleaner : DirectoryCleanerBase
{
    public override string Id => "pipenv";

    public override string Name => "pipenv cache";

    public override string Category => Categories.Python;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        [new CleanupPath(OsPaths.AppCache(context.Environment, Path.Combine("pipenv", "Cache"), "pipenv", "pipenv"))];
}

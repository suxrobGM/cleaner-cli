using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>PDM package/wheel cache. Prefers <c>pdm cache clear</c>.</summary>
public sealed class PdmCleaner : ProcessCleanerBase
{
    public override string Id => "pdm";

    public override string Name => "PDM cache";

    public override string Category => Categories.Python;

    protected override string Executable => "pdm";

    protected override IReadOnlyList<string> CleanArguments => ["cache", "clear"];

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        [new CleanupPath(OsPaths.AppCache(context.Environment, Path.Combine("pdm", "Cache"), "pdm", "pdm"))];
}

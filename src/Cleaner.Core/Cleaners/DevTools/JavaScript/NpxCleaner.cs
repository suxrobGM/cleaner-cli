using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>npx execution cache (npm's <c>_npx</c> directory).</summary>
public sealed class NpxCleaner : DirectoryCleanerBase
{
    public override string Id => "npx";

    public override string Name => "npx cache";

    public override string Category => Categories.JavaScript;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        [new CleanupPath(Path.Combine(NpmCleaner.NpmCacheRoot(context.Environment), "_npx"))];
}

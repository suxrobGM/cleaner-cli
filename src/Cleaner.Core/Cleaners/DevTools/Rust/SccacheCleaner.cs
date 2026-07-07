using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>sccache compilation cache.</summary>
public sealed class SccacheCleaner : DirectoryCleanerBase
{
    public override string Id => "sccache";

    public override string Name => "sccache";

    public override string Category => Categories.Rust;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        [new CleanupPath(OsPaths.AppCache(context.Environment, Path.Combine("Mozilla", "sccache"), "Mozilla.sccache", "sccache"))];
}

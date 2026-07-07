using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>Nim compiler cache (nimcache); installed nimble packages are kept.</summary>
public sealed class NimCleaner : DirectoryCleanerBase
{
    public override string Id => "nim";

    public override string Name => "Nim compiler cache";

    public override string Category => Categories.Languages;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        [new CleanupPath(OsPaths.AppCache(context.Environment, "nim", "nim", "nim"), DeleteMode.ClearContents)];
}

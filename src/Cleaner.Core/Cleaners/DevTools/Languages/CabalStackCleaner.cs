using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>Haskell cabal/stack package caches.</summary>
public sealed class CabalStackCleaner : DirectoryCleanerBase
{
    public override string Id => "haskell";

    public override string Name => "Haskell cabal/stack cache";

    public override string Category => Categories.Languages;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return new CleanupPath(env.HomePath(".cabal", "packages"), Description: "cabal packages");
        yield return new CleanupPath(env.HomePath(".stack", "pantry"), Description: "stack pantry");
    }
}

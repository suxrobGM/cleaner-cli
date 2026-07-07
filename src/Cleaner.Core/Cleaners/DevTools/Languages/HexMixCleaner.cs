using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>Elixir Hex package cache.</summary>
public sealed class HexMixCleaner : DirectoryCleanerBase
{
    public override string Id => "hex";

    public override string Name => "Elixir Hex cache";

    public override string Category => Categories.Languages;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return new CleanupPath(env.HomePath(".hex", "packages"), Description: "Hex packages");
        yield return new CleanupPath(env.HomePath(".mix", "archives"), Description: "Mix archives");
    }
}

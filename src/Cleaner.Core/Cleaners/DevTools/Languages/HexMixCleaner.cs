using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// Elixir Hex package cache. <c>~/.mix/archives</c> is kept: it holds installed archives such as Hex
/// itself and <c>phx_new</c>, which Mix needs to run.
/// </summary>
public sealed class HexMixCleaner : DirectoryCleanerBase
{
    public override string Id => "hex";

    public override string Name => "Elixir Hex cache";

    public override string Category => Categories.Languages;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        [new CleanupPath(context.Environment.HomePath(".hex", "packages"), Description: "Hex packages")];
}

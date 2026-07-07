using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>LuaRocks download/build cache.</summary>
public sealed class LuaRocksCleaner : DirectoryCleanerBase
{
    public override string Id => "luarocks";

    public override string Name => "LuaRocks cache";

    public override string Category => Categories.Languages;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        [new CleanupPath(OsPaths.AppCache(context.Environment, Path.Combine("LuaRocks", "Cache"), "luarocks", "luarocks"))];
}

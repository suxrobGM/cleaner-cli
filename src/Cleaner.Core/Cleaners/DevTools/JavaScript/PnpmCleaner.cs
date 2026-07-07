using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>pnpm content-addressable store. Prefers <c>pnpm store prune</c>.</summary>
public sealed class PnpmCleaner : ProcessCleanerBase
{
    public override string Id => "pnpm";

    public override string Name => "pnpm store";

    public override string Category => Categories.JavaScript;

    protected override string Executable => "pnpm";

    protected override IReadOnlyList<string> CleanArguments => ["store", "prune"];

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return env.IsWindows
            ? new CleanupPath(Path.Combine(env.LocalAppDataDirectory, "pnpm", "store"))
            : new CleanupPath(Path.Combine(env.HomeDirectory, ".local", "share", "pnpm", "store"));
    }
}

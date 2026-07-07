using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// Julia depot caches: precompiled code and logs only. Installed packages, artifacts, and
/// scratchspaces are package-managed state and are deliberately left alone.
/// </summary>
public sealed class JuliaCleaner : DirectoryCleanerBase
{
    public override string Id => "julia";

    public override string Name => "Julia compiled cache";

    public override string Category => Categories.Languages;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;

        // JULIA_DEPOT_PATH is a separator-delimited list; the first entry is the writable depot.
        var depot = OsPaths.Env(env, "JULIA_DEPOT_PATH") is { } configured
            ? configured.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()
            : null;
        depot ??= env.HomePath(".julia");

        yield return new CleanupPath(Path.Combine(depot, "compiled"), Description: "precompiled code");
        yield return new CleanupPath(Path.Combine(depot, "logs"), Description: "logs");
    }
}

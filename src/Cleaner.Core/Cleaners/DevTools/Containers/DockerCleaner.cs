using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// Prunes Docker disk: <c>system prune -a --volumes -f</c> followed by <c>builder prune -af</c>.
/// That reclaims stopped containers, unused networks, every unreferenced image, the build cache and
/// unused named volumes — everything Docker can rebuild or re-pull. On Docker Desktop/WSL2 this only
/// frees space inside the <c>.vhdx</c>; the host file shrinks after a separate compaction.
/// </summary>
public sealed class DockerCleaner : ProcessCleanerBase
{
    public override string Id => "docker";

    public override string Name => "Docker (system prune)";

    public override string Category => Categories.Containers;

    public override bool SupportsSizeEstimate => false;

    protected override string Executable => "docker";

    // The headline command, also used for the dry-run/missing-tool fallback path.
    protected override IReadOnlyList<string> CleanArguments => ["system", "prune", "-a", "--volumes", "--force"];

    protected override IEnumerable<IReadOnlyList<string>> CommandSequence(CleanupContext context)
    {
        yield return CleanArguments;
        yield return ["builder", "prune", "--all", "--force"];
    }

    // Docker's storage lives in the daemon's data root, which isn't a user-accessible directory we
    // can size or delete — the prune commands are the only safe interface.
    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) => [];
}

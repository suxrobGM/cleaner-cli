using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// Prunes Docker disk: <c>system prune -a --volumes -f</c> followed by <c>builder prune -af</c>.
/// That reclaims stopped containers, unused networks, every unreferenced image, the build cache and
/// unused named volumes — everything Docker can rebuild or re-pull. On Docker Desktop/WSL2 this only
/// frees space inside the <c>.vhdx</c>; the host file shrinks after a separate compaction.
/// </summary>
/// <remarks>
/// Nothing here lives in a directory the host can measure, so both the estimate and the freed total
/// come from <c>docker system df</c> — queried before and after the prune. If the daemon is down the
/// numbers are unknown and the UI says so, rather than reporting a misleading zero.
/// </remarks>
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

    public override async Task<ScanResult> ScanAsync(CleanupContext context, CancellationToken cancellationToken = default)
    {
        var usage = await DockerDiskUsage.QueryAsync(context, cancellationToken).ConfigureAwait(false);

        // A clean follows the scan, and the daemon's total is the baseline it needs — keep it.
        RememberMeasurement(context, usage.Used);
        return usage.Reclaimable > 0
            ? new ScanResult([new CleanupTarget("docker", usage.Reclaimable, "unused images, containers, volumes, and build cache")])
            : ScanResult.Empty;
    }

    /// <summary>The daemon's own accounting is the only thing that can size a prune.</summary>
    protected override async ValueTask<long?> MeasureAsync(CleanupContext context, CancellationToken cancellationToken)
    {
        var usage = await DockerDiskUsage.QueryAsync(context, cancellationToken).ConfigureAwait(false);
        return usage.Used;
    }
}

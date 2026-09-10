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
        return usage.Reclaimable > 0
            ? new ScanResult([new CleanupTarget("docker", usage.Reclaimable, "unused images, containers, volumes, and build cache")])
            : ScanResult.Empty;
    }

    public override async Task<CleanResult> CleanAsync(
        CleanupContext context,
        IProgress<CleanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (context.DryRun || !context.ProcessRunner.Exists(Executable))
        {
            return await base.CleanAsync(context, progress, cancellationToken).ConfigureAwait(false);
        }

        var before = await DockerDiskUsage.QueryAsync(context, cancellationToken).ConfigureAwait(false);
        var result = await base.CleanAsync(context, progress, cancellationToken).ConfigureAwait(false);
        var after = await DockerDiskUsage.QueryAsync(context, cancellationToken).ConfigureAwait(false);

        var freed = Math.Max(0, before.Used - after.Used);
        progress?.Report(new CleanProgress(Name, freed));
        return result with { BytesFreed = freed, ItemsRemoved = freed > 0 ? 1 : result.ItemsRemoved };
    }
}

using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// Prunes unused Docker containers, networks, images, volumes, and build cache. On Docker
/// Desktop/WSL2 this frees space inside the <c>.vhdx</c>; compacting the host file is separate.
/// </summary>
/// <remarks>
/// Both estimates and freed totals come from <c>docker system df</c>. If the daemon is unavailable,
/// the cleaner reports an unknown size.
/// </remarks>
public sealed class DockerCleaner : ProcessCleanerBase
{
    public override string Id => "docker";

    public override string Name => "Docker (system prune)";

    public override string Category => Categories.Containers;

    public override bool SupportsSizeEstimate => false;

    protected override string Executable => "docker";

    protected override IReadOnlyList<string> CleanArguments => ["system", "prune", "-a", "--volumes", "--force"];

    protected override IEnumerable<IReadOnlyList<string>> CommandSequence(CleanupContext context)
    {
        yield return CleanArguments;
        yield return ["builder", "prune", "--all", "--force"];
    }

    public override async Task<ScanResult> ScanAsync(CleanupContext context, CancellationToken cancellationToken = default)
    {
        var usage = await DockerDiskUsage.QueryAsync(context, cancellationToken).ConfigureAwait(false);

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

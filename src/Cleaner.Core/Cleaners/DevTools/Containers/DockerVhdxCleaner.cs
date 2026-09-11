using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// Compacts Docker Desktop's WSL2 virtual disks after pruning frees space inside them.
/// </summary>
/// <remarks>
/// WSL is shut down before each disk is compacted with the built-in <c>diskpart</c> utility;
/// compacting an attached disk could corrupt it.
/// </remarks>
public sealed class DockerVhdxCleaner : DirectoryCleanerBase
{
    public override string Id => "docker-vhdx";

    public override string Name => "Docker virtual disk (compact)";

    public override string Category => Categories.Containers;

    public override bool RequiresElevation => true;

    public override bool SupportsSizeEstimate => false;

    /// <summary>Compacts every virtual disk it finds, not the single estimate row a scan reports.</summary>
    public override bool SupportsPartialSelection => false;

    public override string ConfirmationWarning =>
        "this shuts down WSL and every container with it — quit Docker Desktop first, and run " +
        "'docker system prune' beforehand so the space being compacted away is actually free";

    public override bool IsApplicable(CleanupContext context) => context.Environment.IsWindows;

    /// <summary>
    /// Estimates reclaimable slack as host disk size minus Docker's reported usage.
    /// </summary>
    public override async Task<ScanResult> ScanAsync(CleanupContext context, CancellationToken cancellationToken = default)
    {
        var disks = VirtualDisks(context).ToList();
        if (disks.Count == 0)
        {
            return ScanResult.Empty;
        }

        var usage = await DockerDiskUsage.QueryAsync(context, cancellationToken).ConfigureAwait(false);
        if (usage is not { Used: > 0 } known)
        {
            return ScanResult.Empty;
        }

        var onHost = disks.Sum(context.FileSystem.GetFileSize);
        var slack = Math.Max(0, onHost - known.Used);
        return slack > 0
            ? new ScanResult([new CleanupTarget(disks[0], slack, "estimated slack in the Docker virtual disk")])
            : ScanResult.Empty;
    }

    public override async Task<CleanResult> CleanAsync(
        CleanupContext context,
        IProgress<CleanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var disks = VirtualDisks(context).ToList();
        if (disks.Count == 0 || context.DryRun)
        {
            return new CleanResult(0, 0, []);
        }

        var errors = new List<string>();
        var shutdown = await context.ProcessRunner
            .RunAsync("wsl", ["--shutdown"], cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (!shutdown.Success)
        {
            return new CleanResult(0, 0, ["wsl --shutdown failed; disks left untouched to avoid corrupting them."]);
        }

        long freed = 0;
        var compacted = 0;
        foreach (var disk in disks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var before = context.FileSystem.GetFileSize(disk);

            var result = await CompactAsync(context, disk, cancellationToken).ConfigureAwait(false);
            if (!result.Success)
            {
                errors.Add($"{disk}: {result.FailureMessage("diskpart")}");
                continue;
            }

            var saved = Math.Max(0, before - context.FileSystem.GetFileSize(disk));
            freed += saved;
            compacted++;
            progress?.Report(new CleanProgress(disk, saved));
        }

        return new CleanResult(freed, compacted, errors);
    }

    /// <summary>Available once Docker Desktop has created at least one WSL disk.</summary>
    public override bool IsAvailable(CleanupContext context) => VirtualDisks(context).Any();

    private static IEnumerable<string> VirtualDisks(CleanupContext context)
    {
        var root = Path.Combine(context.Environment.LocalAppDataDirectory, "Docker", "wsl");
        return context.FileSystem.DirectoryExists(root)
            ? context.FileSystem.EnumerateFiles(root, "*.vhdx", recursive: true)
            : [];
    }

    /// <summary>
    /// Runs diskpart's attach/compact/detach sequence from a temporary script.
    /// </summary>
    private static async Task<Services.ProcessResult> CompactAsync(
        CleanupContext context,
        string disk,
        CancellationToken cancellationToken)
    {
        var script = Path.Combine(context.Environment.TempDirectory, $"cleaner-compact-{Guid.NewGuid():N}.txt");
        context.FileSystem.WriteAllText(
            script,
            $"""
             select vdisk file="{disk}"
             attach vdisk readonly
             compact vdisk
             detach vdisk
             exit
             """);

        try
        {
            return await context.ProcessRunner
                .RunAsync("diskpart", ["/s", script], cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            context.FileSystem.DeleteFile(script);
        }
    }
}

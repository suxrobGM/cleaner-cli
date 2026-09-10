using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// Compacts the virtual disks Docker Desktop keeps for its WSL2 backend. <c>docker system prune</c>
/// frees space <em>inside</em> the disk, but the host <c>.vhdx</c> only ever grows — it is routinely
/// the largest single file on a developer's machine long after the images it held were deleted.
/// </summary>
/// <remarks>
/// Nothing is deleted here. WSL is shut down and each disk is compacted in place with
/// <c>diskpart</c>, which is present on every Windows install (unlike the Hyper-V
/// <c>Optimize-VHD</c> cmdlet). Compacting a disk that is still attached would corrupt it, hence
/// the shutdown, the elevation requirement, and the confirmation.
/// </remarks>
public sealed class DockerVhdxCleaner : DirectoryCleanerBase
{
    public override string Id => "docker-vhdx";

    public override string Name => "Docker virtual disk (compact)";

    public override string Category => Categories.Containers;

    public override bool RequiresElevation => true;

    // The reclaimable amount is the disk's internal free space, which can't be read from the host.
    // Report it after compacting instead of guessing.
    public override bool SupportsSizeEstimate => false;

    public override string ConfirmationWarning =>
        "this shuts down WSL and every container with it — quit Docker Desktop first, and run " +
        "'docker system prune' beforehand so the space being compacted away is actually free";

    public override bool IsApplicable(CleanupContext context) => context.Environment.IsWindows;

    /// <summary>Report nothing up front: the file's size is not the amount that can be reclaimed.</summary>
    public override Task<ScanResult> ScanAsync(CleanupContext context, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ScanResult([]));

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
            .RunAsync("wsl", ["--shutdown"], cancellationToken)
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
                errors.Add($"{disk}: {Describe(result.StandardError, result.ExitCode)}");
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

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) => [];

    private static IEnumerable<string> VirtualDisks(CleanupContext context)
    {
        var root = Path.Combine(context.Environment.LocalAppDataDirectory, "Docker", "wsl");
        return context.FileSystem.DirectoryExists(root)
            ? context.FileSystem.EnumerateFiles(root, "*.vhdx", recursive: true)
            : [];
    }

    /// <summary>
    /// Drive diskpart through a scripted attach/compact/detach. It only reads a script from a file,
    /// so one is written to temp and removed afterwards.
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
                .RunAsync("diskpart", ["/s", script], cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            context.FileSystem.DeleteFile(script);
        }
    }

    private static string Describe(string standardError, int exitCode) =>
        string.IsNullOrWhiteSpace(standardError) ? $"diskpart exited with code {exitCode}" : standardError.Trim();
}

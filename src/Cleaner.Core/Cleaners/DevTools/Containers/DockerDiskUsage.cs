using Cleaner.Core.Abstractions;
using Cleaner.Core.Utils;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>Disk usage from <c>docker system df</c>, used to size both Docker cleaners.</summary>
/// <param name="Used">Bytes held by images, containers, volumes, and build cache.</param>
/// <param name="Reclaimable">Unreferenced bytes, excluding volumes because the prune keeps them.</param>
internal readonly record struct DockerDiskUsage(long Used, long Reclaimable)
{
    private static readonly TimeSpan QueryTimeout = TimeSpan.FromSeconds(15);

    /// <summary>Returns null if the CLI is missing, the daemon is down, or the output is invalid.</summary>
    public static async Task<DockerDiskUsage?> QueryAsync(CleanupContext context, CancellationToken cancellationToken)
    {
        if (!context.ProcessRunner.Exists("docker"))
        {
            return null;
        }

        // One line per resource type, e.g. "Images|5.1GB|3.2GB (62%)". Docker prints decimal units.
        var result = await context.ProcessRunner
            .RunAsync(
                "docker",
                ["system", "df", "--format", "{{.Type}}|{{.Size}}|{{.Reclaimable}}"],
                QueryTimeout,
                cancellationToken)
            .ConfigureAwait(false);

        if (!result.Success)
        {
            return null;
        }

        long used = 0;
        long reclaimable = 0;
        foreach (var line in result.StandardOutput.Split('\n'))
        {
            var columns = line.Split('|');
            if (columns.Length != 3)
            {
                continue;
            }

            if (SizeParser.TryParse(columns[1], out var size, unitBase: 1000))
            {
                used += size;
            }

            if (!IsVolumes(columns[0]) && SizeParser.TryParse(columns[2], out var free, unitBase: 1000))
            {
                reclaimable += free;
            }
        }

        return new DockerDiskUsage(used, reclaimable);
    }

    /// <summary>The "Local Volumes" row: named volumes are user data the prune leaves alone.</summary>
    private static bool IsVolumes(string type) =>
        type.Contains("Volume", StringComparison.OrdinalIgnoreCase);
}

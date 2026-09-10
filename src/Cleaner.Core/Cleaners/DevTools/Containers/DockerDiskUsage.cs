using Cleaner.Core.Abstractions;
using Cleaner.Core.Services;
using Cleaner.Core.Utils;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// Usage and reclaimable bytes reported by <c>docker system df</c>, used to size both Docker cleaners.
/// </summary>
/// <param name="Used">Bytes of images, containers, volumes and build cache Docker is holding.</param>
/// <param name="Reclaimable">The part of <paramref name="Used"/> nothing references any more.</param>
internal readonly record struct DockerDiskUsage(long Used, long Reclaimable)
{
    private static readonly TimeSpan QueryTimeout = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Queries the daemon; null indicates a missing CLI, unavailable daemon, or invalid output.
    /// </summary>
    public static async Task<DockerDiskUsage?> QueryAsync(CleanupContext context, CancellationToken cancellationToken)
    {
        if (!context.ProcessRunner.Exists("docker"))
        {
            return null;
        }

        // One line per resource type, e.g. "5.1GB|3.2GB (62%)". Docker prints decimal units.
        var result = await context.ProcessRunner
            .RunWithTimeoutAsync(
                "docker",
                ["system", "df", "--format", "{{.Size}}|{{.Reclaimable}}"],
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
            if (columns.Length != 2)
            {
                continue;
            }

            if (SizeParser.TryParse(columns[0], out var size, unitBase: 1000))
            {
                used += size;
            }

            if (SizeParser.TryParse(columns[1], out var free, unitBase: 1000))
            {
                reclaimable += free;
            }
        }

        return new DockerDiskUsage(used, reclaimable);
    }
}

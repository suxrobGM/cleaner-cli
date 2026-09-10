using Cleaner.Core.Abstractions;
using Cleaner.Core.Utils;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// What <c>docker system df</c> reports: bytes currently held, and the share of them a prune would
/// reclaim. Both Docker cleaners size themselves from this, which is the only way either can report
/// a number before it runs.
/// </summary>
/// <param name="Used">Bytes of images, containers, volumes and build cache Docker is holding.</param>
/// <param name="Reclaimable">The part of <paramref name="Used"/> nothing references any more.</param>
internal readonly record struct DockerDiskUsage(long Used, long Reclaimable)
{
    private static readonly DockerDiskUsage Unknown = new(0, 0);

    /// <summary>
    /// Ask the daemon for its disk usage. Returns zeroes when the CLI is missing, the daemon is
    /// down, or the output does not parse — callers report "unknown" rather than a wrong number.
    /// </summary>
    public static async Task<DockerDiskUsage> QueryAsync(CleanupContext context, CancellationToken cancellationToken)
    {
        if (!context.ProcessRunner.Exists("docker"))
        {
            return Unknown;
        }

        // One line per resource type, e.g. "5.1GB|3.2GB (62%)". Docker prints decimal units.
        var result = await context.ProcessRunner
            .RunAsync("docker", ["system", "df", "--format", "{{.Size}}|{{.Reclaimable}}"], cancellationToken)
            .ConfigureAwait(false);

        if (!result.Success)
        {
            return Unknown;
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

using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Utils;

namespace Cleaner.Core.Cleaners.Os;

/// <summary>
/// Finishes deletes an earlier run started but couldn't complete. Those folders were already renamed
/// aside (<c>name.cleaner-deleting-xxxx</c>) and recorded in <c>~/.cleaner/pending-deletes.txt</c>,
/// so nothing at an original path depends on them.
/// </summary>
public sealed class StagedLeftoverCleaner : DirectoryCleanerBase
{
    public override string Id => "cleaner-leftovers";

    public override string Name => "Unfinished Cleaner deletes";

    public override string Category => Categories.SystemCaches;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        Pending(context).Select(path => new CleanupPath(path, Description: "unfinished delete"));

    public override async Task<CleanResult> CleanAsync(
        CleanupContext context,
        IReadOnlyList<CleanupTarget> approved,
        IProgress<CleanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var result = await base.CleanAsync(context, approved, progress, cancellationToken).ConfigureAwait(false);
        if (!context.DryRun)
        {
            // Keep only what is still on disk, so finished entries stop being offered.
            var remaining = Pending(context).ToList();
            context.FileSystem.WriteAllText(
                TargetRemover.PendingDeletesFile(context),
                remaining.Count == 0 ? string.Empty : string.Join(Environment.NewLine, remaining) + Environment.NewLine);
        }

        return result;
    }

    /// <summary>Recorded folders that still exist and carry the staging marker; nothing else is trusted.</summary>
    private static IEnumerable<string> Pending(CleanupContext context) =>
        context.FileSystem.ReadAllLines(TargetRemover.PendingDeletesFile(context))
            .Select(line => line.Trim())
            .Where(line => PathComparison.IsRooted(line)
                && PathComparison.Leaf(line).Contains(TargetRemover.StagedMarker, StringComparison.Ordinal)
                && context.FileSystem.DirectoryExists(line))
            .Distinct();
}

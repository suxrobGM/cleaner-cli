using Cleaner.Cli.Rendering;
using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners;
using Cleaner.Core.Services;

namespace Cleaner.Cli.Application;

/// <summary>
/// Orchestrates the interactive menu and the flows behind it (clean / preview / list / update). All
/// rendering is delegated to <see cref="IConsoleRenderer"/>; this type only decides what to do, never
/// how to draw it. The scan/clean pipeline lives in <c>CleanerApp.CleanFlow.cs</c> and the update
/// flow in <c>CleanerApp.Update.cs</c>.
/// </summary>
public sealed partial class CleanerApp(
    ICleanerRegistry registry,
    IConsoleRenderer renderer,
    IEnvironmentService environment,
    CleanupContextFactory contextFactory,
    IUpdateService updateService,
    IAppLogger logger)
{
    private int List()
    {
        var context = contextFactory.Create(new RunOptions());
        var entries = registry.All.Select(c => new CleanerListEntry(c, StatusOf(c, context))).ToList();
        renderer.CleanerList(entries, registry.Categories.Count);
        return 0;
    }

    /// <summary>Flag rows whose cleaner can't pre-measure but would still run a command.</summary>
    private static IReadOnlyList<ScanRow> MarkCommandBased(IReadOnlyList<ScanRow> rows, CleanupContext context) =>
        [.. rows.Select(r => MarkCommandBased(r, context))];

    private static ScanRow MarkCommandBased(ScanRow row, CleanupContext context) =>
        row with { CommandBased = !row.Cleaner.SupportsSizeEstimate && row.Cleaner.IsAvailable(context) };

    private Task<ScanResult> SafeScanAsync(ICleaner cleaner, CleanupContext context, CancellationToken cancellationToken) =>
        CleanerRunner.SafeScanAsync(cleaner, context, logger, cancellationToken);

    private Task<CleanResult> SafeCleanAsync(ICleaner cleaner, CleanupContext context, CancellationToken cancellationToken) =>
        CleanerRunner.SafeCleanAsync(cleaner, context, logger, cancellationToken);

    private CleanerStatus StatusOf(ICleaner cleaner, CleanupContext context)
    {
        if (!cleaner.IsApplicable(context))
        {
            return CleanerStatus.NotApplicable;
        }

        if (cleaner.RequiresElevation && !environment.IsElevated)
        {
            return CleanerStatus.NeedsElevation;
        }

        return cleaner.IsAvailable(context) ? CleanerStatus.Available : CleanerStatus.NotFound;
    }
}

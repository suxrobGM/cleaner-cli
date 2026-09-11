using Cleaner.Core.Abstractions;
using Spectre.Console;

namespace Cleaner.Cli.Rendering;

/// <summary>The live displays: spinners for scans, progress bars for cleans and downloads.</summary>
public sealed partial class ConsoleRenderer
{
    public Task<T> StatusAsync<T>(string status, Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken) =>
        console.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync(status, _ => work(cancellationToken));

    public async Task<IReadOnlyList<ScanRow>> ScanAsync(
        IReadOnlyList<ICleaner> cleaners,
        Func<ICleaner, Task<ScanResult>> scan,
        CancellationToken cancellationToken)
    {
        // Scans are independent; indexed storage preserves caller order as they complete.
        var rows = new ScanRow[cleaners.Count];
        var scanned = 0;
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Clamp(Environment.ProcessorCount, 1, 8),
            CancellationToken = cancellationToken,
        };

        await console.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync($"Scanning… [green]0/{cleaners.Count}[/]", ctx =>
                Parallel.ForEachAsync(Enumerable.Range(0, cleaners.Count), parallelOptions, async (i, _) =>
                {
                    rows[i] = new ScanRow(cleaners[i], await scan(cleaners[i]));
                    ctx.Status(ScanStatus(cleaners, rows, Interlocked.Increment(ref scanned)));
                }));

        return rows;
    }

    /// <summary>Scan progress, naming the last few stragglers since they are what you wait on.</summary>
    private static string ScanStatus(IReadOnlyList<ICleaner> cleaners, ScanRow[] rows, int done)
    {
        // A row is published only once its scan completes, so the gaps are still running.
        var waiting = cleaners.Count - done is > 0 and <= 3
            ? $" [grey](waiting: {string.Join(", ", cleaners.Where((_, i) => rows[i] is null).Select(c => c.Name)).EscapeMarkup()})[/]"
            : string.Empty;

        return $"Scanning… [green]{done}/{cleaners.Count}[/]{waiting}";
    }

    public async Task<IReadOnlyList<CleanRow>> CleanAsync(
        IReadOnlyList<ICleaner> cleaners,
        Func<ICleaner, Task<CleanResult>> clean,
        CancellationToken cancellationToken)
    {
        var results = new List<CleanRow>(cleaners.Count);
        await BarProgress()
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask("Cleaning", maxValue: cleaners.Count);
                foreach (var cleaner in cleaners)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    task.Description = $"Cleaning [green]{cleaner.Name.EscapeMarkup()}[/]";
                    results.Add(new CleanRow(cleaner, await clean(cleaner)));
                    task.Increment(1);
                }

                task.Description = "Done";
            });

        return results;
    }

    public Task DownloadAsync(
        string description,
        Func<IProgress<double>, CancellationToken, Task> work,
        CancellationToken cancellationToken) =>
        BarProgress()
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask(description, maxValue: 1.0);
                var progress = new Progress<double>(value => task.Value = value);
                await work(progress, cancellationToken);
                task.Value = 1.0;
            });

    /// <summary>A progress display with the standard description / bar / percentage / spinner columns.</summary>
    private Progress BarProgress() =>
        console.Progress()
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn());
}

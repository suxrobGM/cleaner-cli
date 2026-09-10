using Cleaner.Core.Abstractions;

namespace Cleaner.Core.Cleaners.Base;

/// <summary>
/// Base for cleaners driven by an external command. Uses the command when available and falls back
/// to declared directories otherwise.
/// </summary>
public abstract class ProcessCleanerBase : DirectoryCleanerBase
{
    private CleanupContext? _measuredContext;
    private long? _measuredBytes;

    /// <summary>The executable to invoke (resolved on PATH), e.g. "dotnet", "npm", "docker".</summary>
    protected abstract string Executable { get; }

    /// <summary>Arguments passed to <see cref="Executable"/> to perform the cleanup.</summary>
    protected abstract IReadOnlyList<string> CleanArguments { get; }

    /// <summary>Commands to run in order. Override to issue several or to vary them by context.</summary>
    protected virtual IEnumerable<IReadOnlyList<string>> CommandSequence(CleanupContext context) => [CleanArguments];

    public override bool IsAvailable(CleanupContext context) =>
        context.ProcessRunner.Exists(Executable) || base.IsAvailable(context);

    public override async Task<CleanResult> CleanAsync(
        CleanupContext context,
        IProgress<CleanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (context.DryRun || !context.ProcessRunner.Exists(Executable))
        {
            return await base.CleanAsync(context, progress, cancellationToken).ConfigureAwait(false);
        }

        var before = await BaselineAsync(context, cancellationToken).ConfigureAwait(false);
        foreach (var arguments in CommandSequence(context))
        {
            var result = await context.ProcessRunner
                .RunAsync(Executable, arguments, cancellationToken)
                .ConfigureAwait(false);

            if (!result.Success)
            {
                // A command failed — fall back to direct deletion so the user still gets results.
                var fallback = await base.CleanAsync(context, progress, cancellationToken).ConfigureAwait(false);
                return fallback with
                {
                    Errors = [.. fallback.Errors, $"{Executable}: {result.FailureMessage(Executable)}"],
                };
            }
        }

        var after = await MeasureAsync(context, cancellationToken).ConfigureAwait(false);
        var freed = before is { } start && after is { } end ? Math.Max(0, start - end) : 0;
        progress?.Report(new CleanProgress(Name, freed));
        return new CleanResult(freed, 1, []);
    }

    /// <summary>
    /// Hand the clean the size a scan has just measured, so a run that scans first does not pay
    /// for the same measurement twice. Only the context that produced the number can spend it, and
    /// only once, so a later run always measures afresh.
    /// </summary>
    protected void RememberMeasurement(CleanupContext context, long? bytes)
    {
        _measuredContext = context;
        _measuredBytes = bytes;
    }

    /// <summary>The pre-clean size: whatever the scan remembered for this run, else a fresh measure.</summary>
    private async ValueTask<long?> BaselineAsync(CleanupContext context, CancellationToken cancellationToken)
    {
        var remembered = _measuredBytes;
        var matches = ReferenceEquals(_measuredContext, context);

        // Spent either way, so a stale number can never be read and the context is not held on.
        _measuredContext = null;
        _measuredBytes = null;

        return matches ? remembered : await MeasureAsync(context, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Bytes the cleanup is accountable for, measured before and after the command so the summary
    /// reports what was really given back. Defaults to the declared directories; override when only
    /// the tool itself can say (<c>docker system df</c>, DISM's component store report). Return
    /// null when the number cannot be had — the run then reports nothing freed rather than a guess.
    /// </summary>
    protected virtual async ValueTask<long?> MeasureAsync(CleanupContext context, CancellationToken cancellationToken)
    {
        long total = 0;
        foreach (var path in await ExistingTargetsAsync(context, cancellationToken).ConfigureAwait(false))
        {
            total += SizeOf(context, path);
        }

        return total;
    }
}

using Cleaner.Core.Abstractions;

namespace Cleaner.Core.Cleaners.Base;

/// <summary>
/// Base for cleaners driven by an external command. Uses the command when the tool is installed and
/// the declared cache directories only when it is not.
/// </summary>
public abstract class ProcessCleanerBase : DirectoryCleanerBase
{
    private Guid? _measuredRun;
    private long? _measuredBytes;

    /// <summary>The executable to invoke (resolved on PATH), e.g. "dotnet", "npm", "docker".</summary>
    protected abstract string Executable { get; }

    /// <summary>Arguments passed to <see cref="Executable"/> to perform the cleanup.</summary>
    protected abstract IReadOnlyList<string> CleanArguments { get; }

    /// <summary>Commands to run in order. Override to issue several or to vary them by context.</summary>
    protected virtual IEnumerable<IReadOnlyList<string>> CommandSequence(CleanupContext context) => [CleanArguments];

    /// <summary>
    /// How long each command may run before it is killed and reported as a failure. Null waits for
    /// as long as the tool takes; override wherever a wedged tool would otherwise hang the run.
    /// </summary>
    protected virtual TimeSpan? CommandTimeout => null;

    public override bool IsAvailable(CleanupContext context) =>
        context.ProcessRunner.Exists(Executable) || base.IsAvailable(context);

    /// <summary>The tool clears its own cache wholesale; it takes no list of folders to spare.</summary>
    public override bool SupportsPartialSelection => false;

    /// <summary>
    /// Runs the tool's own cleanup whenever the tool is installed: it knows which of its folders
    /// are safe to remove. A failed command is reported and nothing is deleted in its place. Only
    /// when the tool is gone are the approved cache folders removed directly.
    /// </summary>
    public override async Task<CleanResult> CleanAsync(
        CleanupContext context,
        IReadOnlyList<CleanupTarget> approved,
        IProgress<CleanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (context.DryRun || !context.ProcessRunner.Exists(Executable))
        {
            return await base.CleanAsync(context, approved, progress, cancellationToken).ConfigureAwait(false);
        }

        var before = await BaselineAsync(context, cancellationToken).ConfigureAwait(false);
        var outcomes = new List<TargetOutcome>();
        var errors = new List<string>();
        foreach (var arguments in CommandSequence(context))
        {
            var command = string.Join(' ', [Executable, .. arguments]);
            var result = await context.ProcessRunner
                .RunAsync(Executable, arguments, CommandTimeout, cancellationToken)
                .ConfigureAwait(false);

            if (!result.Success)
            {
                var failure = result.FailureMessage(Executable);
                outcomes.Add(new TargetOutcome(command, OutcomeKind.Failed, Reason: failure));
                errors.Add($"{command}: {failure}");
                context.Logger?.Error($"[{Id}] command failed: {command}: {failure}");
                break;
            }

            outcomes.Add(new TargetOutcome(command, OutcomeKind.CommandRun));
            context.Logger?.Info($"[{Id}] ran {command}");
        }

        var after = await MeasureAsync(context, cancellationToken).ConfigureAwait(false);
        var freed = before is { } start && after is { } end ? Math.Max(0, start - end) : 0;
        progress?.Report(new CleanProgress(Name, freed));
        return new CleanResult(freed, errors.Count == 0 ? 1 : 0, errors) { Outcomes = outcomes };
    }

    /// <summary>
    /// Hand the clean the size a scan has just measured, so a run that scans first does not pay
    /// for the same measurement twice. Only the run that produced the number can spend it, and
    /// only once, so a later run always measures afresh.
    /// </summary>
    protected void RememberMeasurement(CleanupContext context, long? bytes)
    {
        _measuredRun = context.RunId;
        _measuredBytes = bytes;
    }

    /// <summary>The pre-clean size: whatever the scan remembered for this run, else a fresh measure.</summary>
    private async ValueTask<long?> BaselineAsync(CleanupContext context, CancellationToken cancellationToken)
    {
        var remembered = _measuredBytes;
        var matches = _measuredRun == context.RunId;

        // Spent either way, so a stale number can never be read.
        _measuredRun = null;
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

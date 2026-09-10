using Cleaner.Core.Abstractions;

namespace Cleaner.Core.Cleaners.Base;

/// <summary>
/// For cleaners where an external command is authoritative (<c>pnpm store prune</c>). Runs the
/// command when the tool is present, else deletes the declared directories. Sizing always comes
/// from those directories, so scans report reclaimable space either way.
/// </summary>
public abstract class ProcessCleanerBase : DirectoryCleanerBase
{
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
        // Dry-run or missing tool: measure/delete via the declared directories (base behavior).
        if (context.DryRun || !context.ProcessRunner.Exists(Executable))
        {
            return await base.CleanAsync(context, progress, cancellationToken).ConfigureAwait(false);
        }

        var before = await TotalSizeAsync(context, cancellationToken).ConfigureAwait(false);
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

        var after = await TotalSizeAsync(context, cancellationToken).ConfigureAwait(false);
        var freed = Math.Max(0, before - after);
        progress?.Report(new CleanProgress(Name, freed));
        return new CleanResult(freed, 1, []);
    }

    private async ValueTask<long> TotalSizeAsync(CleanupContext context, CancellationToken cancellationToken)
    {
        long total = 0;
        foreach (var path in await ExistingTargetsAsync(context, cancellationToken).ConfigureAwait(false))
        {
            total += SizeOf(context, path);
        }

        return total;
    }
}

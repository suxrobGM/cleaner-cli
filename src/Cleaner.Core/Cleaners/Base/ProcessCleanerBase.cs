using Cleaner.Core.Abstractions;

namespace Cleaner.Core.Cleaners.Base;

/// <summary>
/// Base class for cleaners where an external command is the authoritative way to clear the cache
/// (e.g. <c>dotnet nuget locals all --clear</c>, <c>pnpm store prune</c>). When the tool is present
/// the command runs; otherwise it falls back to deleting the declared cache directories. Sizing is
/// always done from those directories so scans and progress still report reclaimable space.
/// </summary>
public abstract class ProcessCleanerBase : DirectoryCleanerBase
{
    /// <summary>The executable to invoke (resolved on PATH), e.g. "dotnet", "npm", "docker".</summary>
    protected abstract string Executable { get; }

    /// <summary>Arguments passed to <see cref="Executable"/> to perform the cleanup.</summary>
    protected abstract IReadOnlyList<string> CleanArguments { get; }

    /// <summary>
    /// The command(s) to run, in order. Defaults to a single invocation with <see cref="CleanArguments"/>;
    /// override to issue several commands or to vary them by context (e.g. honoring <c>--force</c>).
    /// </summary>
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

        var before = TotalSize(context);
        foreach (var arguments in CommandSequence(context))
        {
            var result = await context.ProcessRunner
                .RunAsync(Executable, arguments, cancellationToken)
                .ConfigureAwait(false);

            if (!result.Success)
            {
                // A command failed — fall back to direct deletion so the user still gets results.
                var fallback = await base.CleanAsync(context, progress, cancellationToken).ConfigureAwait(false);
                var error = string.IsNullOrWhiteSpace(result.StandardError)
                    ? $"{Executable} exited with code {result.ExitCode}"
                    : result.StandardError.Trim();
                return fallback with { Errors = [.. fallback.Errors, $"{Executable}: {error}"] };
            }
        }

        var after = TotalSize(context);
        var freed = Math.Max(0, before - after);
        progress?.Report(new CleanProgress(Name, freed));
        return new CleanResult(freed, 1, []);
    }

    private long TotalSize(CleanupContext context)
    {
        long total = 0;
        foreach (var path in ExistingTargets(context))
        {
            total += context.FileSystem.GetDirectorySize(path.Path);
        }

        return total;
    }
}

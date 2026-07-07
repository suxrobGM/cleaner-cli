using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>pre-commit hook environment cache (rebuilt on the next run).</summary>
public sealed class PreCommitCleaner : DirectoryCleanerBase
{
    public override string Id => "pre-commit";

    public override string Name => "pre-commit cache";

    public override string Category => Categories.Python;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return new CleanupPath(
            OsPaths.Env(env, "PRE_COMMIT_HOME") ?? env.HomePath(".cache", "pre-commit"));
    }
}

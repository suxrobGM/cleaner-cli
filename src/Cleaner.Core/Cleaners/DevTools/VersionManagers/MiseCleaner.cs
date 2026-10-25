using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// mise (formerly rtx) download/HTTP cache; installed tools under ~/.local/share/mise are kept.
/// Prefers <c>mise cache clear</c>.
/// </summary>
public sealed class MiseCleaner : ProcessCleanerBase
{
    public override string Id => "mise";

    public override string Name => "mise cache";

    public override string Category => Categories.ToolingDownloads;

    protected override string Executable => "mise";

    protected override IReadOnlyList<string> CleanArguments => ["cache", "clear"];

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return new CleanupPath(
            OsPaths.Env(env, "MISE_CACHE_DIR") ?? OsPaths.AppCache(env, Path.Combine("mise", "cache"), "mise", "mise"),
            DeleteMode.ClearContents);
    }
}

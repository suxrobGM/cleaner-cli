using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>Deno module and dependency cache.</summary>
public sealed class DenoCleaner : DirectoryCleanerBase
{
    public override string Id => "deno";

    public override string Name => "Deno cache";

    public override string Category => Categories.JavaScript;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        var denoDir = env.GetEnvironmentVariable("DENO_DIR");
        if (!string.IsNullOrWhiteSpace(denoDir))
        {
            yield return new CleanupPath(denoDir, DeleteMode.ClearContents);
            yield break;
        }

        yield return new CleanupPath(OsPaths.AppCache(env, "deno", "deno", "deno"), DeleteMode.ClearContents);
    }
}

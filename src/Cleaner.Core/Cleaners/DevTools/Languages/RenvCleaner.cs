using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>renv (R) global package cache; per-project libraries re-link or reinstall from it.</summary>
public sealed class RenvCleaner : DirectoryCleanerBase
{
    public override string Id => "renv";

    public override string Name => "renv package cache";

    public override string Category => Categories.Languages;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        if (OsPaths.Env(env, "RENV_PATHS_CACHE") is { } configured)
        {
            yield return new CleanupPath(configured, DeleteMode.ClearContents);
            yield break;
        }

        // renv's documented per-OS cache roots.
        if (env.IsWindows)
        {
            yield return new CleanupPath(Path.Combine(env.LocalAppDataDirectory, "renv", "cache"), DeleteMode.ClearContents);
            yield return new CleanupPath(Path.Combine(env.LocalAppDataDirectory, "R", "cache", "R", "renv"), DeleteMode.ClearContents);
        }
        else if (env.IsMacOs)
        {
            yield return new CleanupPath(
                Path.Combine(env.HomeDirectory, "Library", "Caches", "org.R-project.R", "R", "renv"), DeleteMode.ClearContents);
        }
        else
        {
            yield return new CleanupPath(Path.Combine(env.CacheDirectory, "R", "renv"), DeleteMode.ClearContents);
        }
    }
}

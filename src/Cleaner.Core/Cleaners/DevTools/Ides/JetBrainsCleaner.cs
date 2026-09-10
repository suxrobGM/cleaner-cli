using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>JetBrains IDE caches and logs (IntelliJ, Rider, PyCharm, ...).</summary>
public sealed class JetBrainsCleaner : DirectoryCleanerBase
{
    public override string Id => "jetbrains";

    public override string Name => "JetBrains IDE caches";

    public override string Category => Categories.Ides;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        if (!env.IsWindows)
        {
            // ~/Library/Caches/JetBrains and ~/.cache/JetBrains hold only derived data.
            yield return new CleanupPath(OsPaths.AppCache(env, "JetBrains", "JetBrains", "JetBrains"), DeleteMode.ClearContents);
            yield break;
        }

        // On Windows %LOCALAPPDATA%\JetBrains also contains Toolbox (installed IDE binaries) and
        // per-product state such as LocalHistory, so only the cache subdirs of each product dir go.
        var root = Path.Combine(env.LocalAppDataDirectory, "JetBrains");
        foreach (var productDir in context.FileSystem.EnumerateDirectories(root))
        {
            if (DirectorySweep.LeafName(productDir).Equals("Toolbox", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var path in JetBrainsCache.Under(productDir, DirectorySweep.LeafName(productDir)))
            {
                yield return path;
            }
        }
    }
}

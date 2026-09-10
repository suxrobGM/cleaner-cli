using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// Android Studio caches. It uses the JetBrains layout but lives under a <c>Google</c> root, so the
/// <c>jetbrains</c> cleaner never sees it, and every upgrade leaves the previous version's caches
/// behind. Installed plugins and settings are kept.
/// </summary>
public sealed class AndroidStudioCleaner : DirectoryCleanerBase
{
    public override string Id => "android-studio";

    public override string Name => "Android Studio caches";

    public override string Category => Categories.Ides;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        foreach (var root in VersionRoots(context))
        {
            // Android Studio adds the JPS build-server caches on top of the shared layout.
            foreach (var path in JetBrainsCache.Under(
                root, DirectorySweep.LeafName(root), "compile-server", "compiler"))
            {
                yield return path;
            }
        }
    }

    /// <summary>Every installed version's data directory, across the per-OS Google roots.</summary>
    private static IEnumerable<string> VersionRoots(CleanupContext context)
    {
        var env = context.Environment;
        string[] roots =
        [
            OsPaths.AppCache(env, "Google", "Google", "Google"),
            OsPaths.AppData(env, "Google", "Google", "Google"),
        ];

        return roots
            .SelectMany(context.FileSystem.EnumerateDirectories)
            .Where(dir => DirectorySweep.LeafName(dir).StartsWith("AndroidStudio", StringComparison.OrdinalIgnoreCase));
    }
}

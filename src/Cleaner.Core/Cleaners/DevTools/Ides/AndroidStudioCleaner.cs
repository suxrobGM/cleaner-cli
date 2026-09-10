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
    /// <summary>Derived data under a per-version dir; everything else may be settings or plugins.</summary>
    private static readonly string[] CacheSubdirectories =
        ["caches", "index", "log", "tmp", "compile-server", "compiler"];

    public override string Id => "android-studio";

    public override string Name => "Android Studio caches";

    public override string Category => Categories.Ides;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        foreach (var root in VersionRoots(context))
        {
            var version = DirectorySweep.LeafName(root);
            foreach (var sub in CacheSubdirectories)
            {
                yield return new CleanupPath(Path.Combine(root, sub), Description: $"{version} {sub}");
            }
        }
    }

    /// <summary>Every installed version's data directory, across the per-OS Google roots.</summary>
    private static IEnumerable<string> VersionRoots(CleanupContext context)
    {
        var env = context.Environment;
        string[] roots = env.IsWindows
            ? [Path.Combine(env.LocalAppDataDirectory, "Google"), Path.Combine(env.AppDataDirectory, "Google")]
            : env.IsMacOs
                ? [Path.Combine(env.HomeDirectory, "Library", "Caches", "Google"),
                   Path.Combine(env.HomeDirectory, "Library", "Application Support", "Google")]
                : [Path.Combine(env.CacheDirectory, "Google"), env.HomePath(".config", "Google")];

        return roots
            .SelectMany(context.FileSystem.EnumerateDirectories)
            .Where(dir => DirectorySweep.LeafName(dir).StartsWith("AndroidStudio", StringComparison.OrdinalIgnoreCase));
    }
}

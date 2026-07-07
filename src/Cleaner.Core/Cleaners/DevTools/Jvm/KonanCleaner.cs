using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// Kotlin/Native (konan) caches: compiler caches, toolchain dependencies, and auto-downloaded
/// compiler distributions. The Kotlin Gradle plugin re-fetches whatever a build needs.
/// </summary>
public sealed class KonanCleaner : DirectoryCleanerBase
{
    public override string Id => "konan";

    public override string Name => "Kotlin/Native cache";

    public override string Category => Categories.Jvm;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var root = context.Environment.HomePath(".konan");
        yield return new CleanupPath(Path.Combine(root, "cache"), Description: "compiler cache");
        yield return new CleanupPath(Path.Combine(root, "dependencies"), Description: "toolchain dependencies");

        // Downloaded compiler distributions (kotlin-native-prebuilt-*, kotlin-native-*) — re-fetched on demand.
        foreach (var dir in context.FileSystem.EnumerateDirectories(root))
        {
            if (DirectorySweep.LeafName(dir).StartsWith("kotlin-native-", StringComparison.OrdinalIgnoreCase))
            {
                yield return new CleanupPath(dir, Description: "compiler distribution");
            }
        }
    }
}

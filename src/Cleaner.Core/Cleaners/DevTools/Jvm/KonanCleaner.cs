using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Utils;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>Kotlin/Native compiler cache, toolchain, and downloaded compilers (re-fetched by builds).</summary>
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

        foreach (var dir in context.FileSystem.EnumerateDirectories(root))
        {
            if (PathComparison.Name(dir).StartsWith("kotlin-native-", StringComparison.OrdinalIgnoreCase))
            {
                yield return new CleanupPath(dir, Description: "compiler distribution");
            }
        }
    }
}

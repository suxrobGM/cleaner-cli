using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// React Native caches: Metro bundler / Haste temp directories and the CocoaPods cache (macOS).
/// </summary>
public sealed class ReactNativeCleaner : DirectoryCleanerBase
{
    private static readonly string[] TempPrefixes = ["metro-", "react-", "haste-map-"];

    public override string Id => "react-native";

    public override string Name => "React Native (Metro) caches";

    public override string Category => Categories.Mobile;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        var temp = env.TempDirectory;

        foreach (var dir in context.FileSystem.EnumerateDirectories(temp))
        {
            var name = Path.GetFileName(dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (TempPrefixes.Any(p => name.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            {
                yield return new CleanupPath(dir, Description: "Metro temp cache");
            }
        }

        if (env.IsMacOs)
        {
            yield return new CleanupPath(
                Path.Combine(env.HomeDirectory, "Library", "Caches", "CocoaPods"),
                Description: "CocoaPods cache");
        }
    }
}

/// <summary>Expo caches: the global <c>~/.expo</c> store and the project-local <c>.expo</c> folder.</summary>
public sealed class ExpoCleaner : DirectoryCleanerBase
{
    public override string Id => "expo";

    public override string Name => "Expo caches";

    public override string Category => Categories.Mobile;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        yield return new CleanupPath(context.Environment.HomePath(".expo"), DeleteMode.ClearContents, "global cache");
        yield return new CleanupPath(Path.Combine(context.WorkingDirectory, ".expo"), Description: "project cache");
    }
}

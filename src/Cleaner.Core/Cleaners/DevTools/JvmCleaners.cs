using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>Gradle build caches and downloaded dependencies.</summary>
public sealed class GradleCleaner : DirectoryCleanerBase
{
    public override string Id => "gradle";

    public override string Name => "Gradle caches";

    public override string Category => Categories.Jvm;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        var gradleHome = OsPaths.Env(env, "GRADLE_USER_HOME") ?? env.HomePath(".gradle");
        yield return new CleanupPath(Path.Combine(gradleHome, "caches"));
    }
}

/// <summary>Maven local repository.</summary>
public sealed class MavenCleaner : DirectoryCleanerBase
{
    public override string Id => "maven";

    public override string Name => "Maven repository";

    public override string Category => Categories.Jvm;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        [new CleanupPath(context.Environment.HomePath(".m2", "repository"))];
}

/// <summary>sbt / Ivy resolution caches.</summary>
public sealed class SbtIvyCleaner : DirectoryCleanerBase
{
    public override string Id => "sbt";

    public override string Name => "sbt / Ivy cache";

    public override string Category => Categories.Jvm;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return new CleanupPath(env.HomePath(".ivy2", "cache"), Description: "Ivy cache");
        yield return new CleanupPath(env.HomePath(".sbt", "boot"), Description: "sbt boot");
    }
}

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

/// <summary>Android SDK build/manager caches (keeps installed SDKs and AVDs).</summary>
public sealed class AndroidSdkCleaner : DirectoryCleanerBase
{
    public override string Id => "android";

    public override string Name => "Android caches";

    public override string Category => Categories.Jvm;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return new CleanupPath(env.HomePath(".android", "cache"), Description: "SDK cache");
        yield return new CleanupPath(env.HomePath(".android", "build-cache"), Description: "build cache");
    }
}

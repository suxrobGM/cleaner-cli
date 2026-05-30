using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>Gradle build caches and downloaded dependencies.</summary>
public sealed class GradleCleaner : DirectoryCleanerBase
{
    public override string Id => "gradle";

    public override string Name => "Gradle caches";

    public override string Category => Categories.Jvm;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        [new CleanupPath(context.Environment.HomePath(".gradle", "caches"))];
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

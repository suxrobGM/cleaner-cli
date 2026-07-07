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
        yield return new CleanupPath(Path.Combine(gradleHome, "caches"), Description: "build caches");
        yield return new CleanupPath(Path.Combine(gradleHome, "wrapper", "dists"), Description: "wrapper distributions");
        yield return new CleanupPath(Path.Combine(gradleHome, "daemon"), Description: "daemon logs");
    }
}

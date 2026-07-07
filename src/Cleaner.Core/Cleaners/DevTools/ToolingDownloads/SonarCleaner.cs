using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>SonarLint / sonar-scanner plugin and analyzer cache.</summary>
public sealed class SonarCleaner : DirectoryCleanerBase
{
    public override string Id => "sonar";

    public override string Name => "Sonar scanner cache";

    public override string Category => Categories.ToolingDownloads;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        var root = OsPaths.Env(env, "SONAR_USER_HOME") ?? env.HomePath(".sonar");
        yield return new CleanupPath(Path.Combine(root, "cache"), DeleteMode.ClearContents);
    }
}

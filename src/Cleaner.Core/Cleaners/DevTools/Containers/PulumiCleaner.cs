using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>Pulumi provider plugin binaries (re-downloaded on demand; keeps stacks and config).</summary>
public sealed class PulumiCleaner : DirectoryCleanerBase
{
    public override string Id => "pulumi";

    public override string Name => "Pulumi plugin cache";

    public override string Category => Categories.Containers;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        var root = OsPaths.Env(env, "PULUMI_HOME") ?? env.HomePath(".pulumi");
        yield return new CleanupPath(Path.Combine(root, "plugins"), DeleteMode.ClearContents);
    }
}

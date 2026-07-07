using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>minikube image/ISO download cache (keeps profiles and VMs).</summary>
public sealed class MinikubeCleaner : DirectoryCleanerBase
{
    public override string Id => "minikube";

    public override string Name => "minikube cache";

    public override string Category => Categories.Containers;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        var root = OsPaths.Env(env, "MINIKUBE_HOME") ?? env.HomePath(".minikube");
        yield return new CleanupPath(Path.Combine(root, "cache"));
    }
}

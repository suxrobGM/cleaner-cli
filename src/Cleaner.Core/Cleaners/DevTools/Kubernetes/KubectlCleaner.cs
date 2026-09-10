using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>kubectl discovery and HTTP caches (kubeconfig and credentials untouched).</summary>
public sealed class KubectlCleaner : DirectoryCleanerBase
{
    public override string Id => "kubectl";

    public override string Name => "kubectl caches";

    public override string Category => Categories.Containers;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return new CleanupPath(env.HomePath(".kube", "cache"), Description: "discovery cache");
        yield return new CleanupPath(env.HomePath(".kube", "http-cache"), Description: "HTTP cache");
    }
}

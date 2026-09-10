using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>Ansible temp workspace and Galaxy download cache.</summary>
public sealed class AnsibleCleaner : DirectoryCleanerBase
{
    public override string Id => "ansible";

    public override string Name => "Ansible caches";

    public override string Category => Categories.Containers;

    public override bool IsApplicable(CleanupContext context) => !context.Environment.IsWindows;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return new CleanupPath(env.HomePath(".ansible", "tmp"), Description: "temp workspace");
        yield return new CleanupPath(env.HomePath(".ansible", "galaxy_cache"), Description: "Galaxy cache");
    }
}

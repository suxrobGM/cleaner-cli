using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>Vagrant temp downloads only — boxes are user data and are never touched.</summary>
public sealed class VagrantCleaner : DirectoryCleanerBase
{
    public override string Id => "vagrant";

    public override string Name => "Vagrant temp downloads";

    public override string Category => Categories.Containers;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        var root = OsPaths.Env(env, "VAGRANT_HOME") ?? env.HomePath(".vagrant.d");
        yield return new CleanupPath(Path.Combine(root, "tmp"), DeleteMode.ClearContents);
    }
}

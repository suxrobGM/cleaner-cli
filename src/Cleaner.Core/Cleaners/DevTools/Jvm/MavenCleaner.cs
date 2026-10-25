using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>Maven local repository.</summary>
public sealed class MavenCleaner : DirectoryCleanerBase
{
    public override string Id => "maven";

    public override string Name => "Maven repository";

    public override string Category => Categories.Jvm;

    public override string ConfirmationWarning =>
        "the local repository also holds artifacts built on this machine with 'mvn install', which " +
        "can't be re-downloaded — rebuild them before the projects that depend on them will build";

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return new CleanupPath(env.HomePath(".m2", "repository"), Description: "local repository");
        yield return new CleanupPath(env.HomePath(".m2", "wrapper"), Description: "wrapper distributions");
    }
}

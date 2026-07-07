using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

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

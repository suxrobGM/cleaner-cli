using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>Dart/Flutter pub cache (hosted packages, git checkouts, temp).</summary>
public sealed class PubCleaner : DirectoryCleanerBase
{
    public override string Id => "pub";

    public override string Name => "Dart/Flutter pub cache";

    public override string Category => Categories.Languages;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        var root = OsPaths.Env(env, "PUB_CACHE")
            ?? (env.IsWindows
                ? Path.Combine(env.LocalAppDataDirectory, "Pub", "Cache")
                : env.HomePath(".pub-cache"));

        yield return new CleanupPath(Path.Combine(root, "hosted"), Description: "hosted packages");
        yield return new CleanupPath(Path.Combine(root, "git"), Description: "git packages");
        yield return new CleanupPath(Path.Combine(root, ".tmp"), Description: "temp");
    }
}

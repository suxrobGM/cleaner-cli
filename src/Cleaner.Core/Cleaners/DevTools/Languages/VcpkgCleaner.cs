using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>vcpkg download and binary-archive caches (C/C++ package manager).</summary>
public sealed class VcpkgCleaner : DirectoryCleanerBase
{
    public override string Id => "vcpkg";

    public override string Name => "vcpkg cache";

    public override string Category => Categories.Languages;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        if (env.IsWindows)
        {
            var root = Path.Combine(env.LocalAppDataDirectory, "vcpkg");
            yield return new CleanupPath(Path.Combine(root, "downloads"), Description: "downloads");
            yield return new CleanupPath(Path.Combine(root, "archives"), Description: "binary cache");
        }
        else
        {
            yield return new CleanupPath(Path.Combine(env.CacheDirectory, "vcpkg", "archives"), Description: "binary cache");
        }
    }
}

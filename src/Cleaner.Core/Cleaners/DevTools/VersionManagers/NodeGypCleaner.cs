using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>node-gyp's downloaded Node headers and import libraries (re-fetched per version).</summary>
public sealed class NodeGypCleaner : DirectoryCleanerBase
{
    public override string Id => "node-gyp";

    public override string Name => "node-gyp header cache";

    public override string Category => Categories.ToolingDownloads;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return new CleanupPath(OsPaths.AppCache(env, Path.Combine("node-gyp", "Cache"), "node-gyp", "node-gyp"));

        // Older node-gyp versions used ~/.node-gyp on every OS.
        yield return new CleanupPath(env.HomePath(".node-gyp"));
    }
}

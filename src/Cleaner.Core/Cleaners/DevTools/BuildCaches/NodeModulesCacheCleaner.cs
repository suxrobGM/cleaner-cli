using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>The <c>node_modules/.cache</c> directory under the working directory.</summary>
public sealed class NodeModulesCacheCleaner : DirectoryCleanerBase
{
    public override string Id => "node-modules-cache";

    public override string Name => "node_modules/.cache";

    public override string Category => Categories.BuildCaches;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        [new CleanupPath(Path.Combine(context.WorkingDirectory, "node_modules", ".cache"))];
}

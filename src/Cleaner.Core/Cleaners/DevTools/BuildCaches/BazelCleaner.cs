using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>Bazel disk cache and repository cache.</summary>
public sealed class BazelCleaner : DirectoryCleanerBase
{
    public override string Id => "bazel";

    public override string Name => "Bazel cache";

    public override string Category => Categories.BuildCaches;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        [new CleanupPath(Path.Combine(context.Environment.CacheDirectory, "bazel"), DeleteMode.ClearContents)];
}

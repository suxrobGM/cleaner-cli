using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>Ruby Bundler cache.</summary>
public sealed class GemBundlerCleaner : DirectoryCleanerBase
{
    public override string Id => "bundler";

    public override string Name => "Ruby Bundler cache";

    public override string Category => Categories.Languages;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        [new CleanupPath(context.Environment.HomePath(".bundle", "cache"))];
}

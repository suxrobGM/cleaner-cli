using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.Os;

/// <summary>The XDG user cache root ~/.cache (Linux).</summary>
public sealed class XdgCacheCleaner : DirectoryCleanerBase
{
    public override string Id => "xdg-cache";

    public override string Name => "User cache (~/.cache)";

    public override string Category => Categories.OperatingSystem;

    public override bool IsApplicable(CleanupContext context) => context.Environment.IsLinux;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        [new CleanupPath(context.Environment.CacheDirectory, DeleteMode.ClearContents)];
}

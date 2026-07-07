using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>Swift Package Manager repository/artifact caches (macOS and Linux).</summary>
public sealed class SwiftPmCleaner : DirectoryCleanerBase
{
    public override string Id => "swiftpm";

    public override string Name => "Swift Package Manager cache";

    public override string Category => Categories.Languages;

    public override bool IsApplicable(CleanupContext context) => !context.Environment.IsWindows;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return env.IsMacOs
            ? new CleanupPath(Path.Combine(env.HomeDirectory, "Library", "Caches", "org.swift.swiftpm"))
            : new CleanupPath(Path.Combine(env.CacheDirectory, "org.swift.swiftpm"));
    }
}

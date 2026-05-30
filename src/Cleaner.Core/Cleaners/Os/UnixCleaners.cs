using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.Os;

/// <summary>User caches and logs under ~/Library (macOS).</summary>
public sealed class MacUserCachesCleaner : DirectoryCleanerBase
{
    public override string Id => "mac-caches";

    public override string Name => "macOS user caches & logs";

    public override string Category => Categories.OperatingSystem;

    public override bool IsApplicable(CleanupContext context) => context.Environment.IsMacOs;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var home = context.Environment.HomeDirectory;
        yield return new CleanupPath(Path.Combine(home, "Library", "Caches"), DeleteMode.ClearContents, "caches");
        yield return new CleanupPath(Path.Combine(home, "Library", "Logs"), DeleteMode.ClearContents, "logs");
    }
}

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

/// <summary>Vacuums the systemd journal to a small size (Linux). Needs elevation.</summary>
public sealed class JournalLogCleaner : ProcessCleanerBase
{
    public override string Id => "journal";

    public override string Name => "systemd journal logs";

    public override string Category => Categories.OperatingSystem;

    public override bool RequiresElevation => true;

    public override bool IsApplicable(CleanupContext context) => context.Environment.IsLinux;

    protected override string Executable => "journalctl";

    protected override IReadOnlyList<string> CleanArguments => ["--vacuum-size=100M"];

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) => [];
}

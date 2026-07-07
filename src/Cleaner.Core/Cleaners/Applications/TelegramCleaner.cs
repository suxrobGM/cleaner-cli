using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.Applications;

/// <summary>
/// Telegram Desktop media and emoji caches — the same data its own "Clear cache" button removes
/// (re-downloaded on view). Account state under <c>tdata</c> (session keys, settings, drafts) is
/// never touched, so this can't log the user out.
/// </summary>
public sealed class TelegramCleaner : DirectoryCleanerBase
{
    public override string Id => "telegram";

    public override string Name => "Telegram media cache";

    public override string Category => Categories.Applications;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        var root = env.IsWindows
            ? Path.Combine(env.AppDataDirectory, "Telegram Desktop")
            : env.IsMacOs
                ? Path.Combine(env.HomeDirectory, "Library", "Application Support", "Telegram Desktop")
                : Path.Combine(env.HomeDirectory, ".local", "share", "TelegramDesktop");

        var tdata = Path.Combine(root, "tdata");
        yield return new CleanupPath(Path.Combine(tdata, "user_data", "cache"), DeleteMode.ClearContents, "media cache");
        yield return new CleanupPath(Path.Combine(tdata, "user_data", "media_cache"), DeleteMode.ClearContents, "media cache");
        yield return new CleanupPath(Path.Combine(tdata, "emoji"), DeleteMode.ClearContents, "emoji cache");

        // Multi-account: each extra account keeps its caches under user_data#2, user_data#3, ...
        foreach (var dir in context.FileSystem.EnumerateDirectories(tdata))
        {
            if (DirectorySweep.LeafName(dir).StartsWith("user_data#", StringComparison.OrdinalIgnoreCase))
            {
                yield return new CleanupPath(Path.Combine(dir, "cache"), DeleteMode.ClearContents, "media cache");
                yield return new CleanupPath(Path.Combine(dir, "media_cache"), DeleteMode.ClearContents, "media cache");
            }
        }
    }
}

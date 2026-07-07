using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Services;

namespace Cleaner.Core.Cleaners.Os;

/// <summary>Explorer thumbnail and icon cache databases.</summary>
public sealed class ThumbnailCacheCleaner : WindowsCleanerBase
{
    public override string Id => "thumbnails";

    public override string Name => "Thumbnail & icon cache";

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
    [
        new CleanupPath(
            Path.Combine(context.Environment.LocalAppDataDirectory, "Microsoft", "Windows", "Explorer"),
            DeleteMode.ClearContents),
    ];
}

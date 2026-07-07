using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.Applications;

/// <summary>
/// Dropbox's internal <c>.dropbox.cache</c> staging folder (officially safe to purge; synced files
/// are never touched). Only the default <c>~/Dropbox</c> location is covered.
/// </summary>
public sealed class DropboxCleaner : DirectoryCleanerBase
{
    public override string Id => "dropbox";

    public override string Name => "Dropbox internal cache";

    public override string Category => Categories.Applications;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        [new CleanupPath(context.Environment.HomePath("Dropbox", ".dropbox.cache"), DeleteMode.ClearContents)];
}

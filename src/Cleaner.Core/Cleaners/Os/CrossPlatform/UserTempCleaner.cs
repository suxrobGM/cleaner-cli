using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.Os;

/// <summary>Clears the per-user temporary directory.</summary>
public sealed class UserTempCleaner : DirectoryCleanerBase
{
    public override string Id => "temp";

    public override string Name => "User temp files";

    public override string Category => Categories.OperatingSystem;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        [new CleanupPath(context.Environment.TempDirectory, DeleteMode.ClearContents, "temp directory")];
}

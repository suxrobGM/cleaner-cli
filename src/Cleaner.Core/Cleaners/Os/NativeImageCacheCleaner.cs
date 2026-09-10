using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.Os;

/// <summary>
/// The .NET Framework native image cache: the <c>NativeImages_v*</c> trees under
/// <c>C:\Windows\assembly</c>, where NGEN stores ahead-of-time compiled copies of managed
/// assemblies. They are derived from the assemblies in the GAC and Windows rebuilds the ones it
/// still wants through its own NGEN maintenance task.
/// </summary>
/// <remarks>
/// Only the <c>NativeImages_v*</c> directories are touched. The GAC itself (<c>GAC</c>,
/// <c>GAC_32</c>, <c>GAC_64</c>, <c>GAC_MSIL</c>) holds the actual assemblies and is never removed.
/// This applies to .NET Framework only; .NET 5+ does not use NGEN.
/// </remarks>
public sealed class NativeImageCacheCleaner : WindowsCleanerBase
{
    public override string Id => "ngen-cache";

    public override string Name => ".NET Framework native image cache";

    public override bool RequiresElevation => true;

    public override string ConfirmationWarning =>
        "these are rebuilt lazily, so .NET Framework apps (including parts of Windows and Visual " +
        "Studio) start noticeably slower until the NGEN maintenance task catches up";

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var windows = context.Environment.WindowsDirectory;
        if (windows is null)
        {
            yield break;
        }

        var assembly = Path.Combine(windows, "assembly");
        foreach (var directory in context.FileSystem.EnumerateDirectories(assembly))
        {
            var name = DirectorySweep.LeafName(directory);
            if (name.StartsWith("NativeImages_", StringComparison.OrdinalIgnoreCase))
            {
                yield return new CleanupPath(directory, DeleteMode.ClearContents, name);
            }
        }
    }
}

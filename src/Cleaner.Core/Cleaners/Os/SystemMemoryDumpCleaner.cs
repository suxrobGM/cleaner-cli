using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Services;

namespace Cleaner.Core.Cleaners.Os;

/// <summary>System crash memory dumps (kernel minidumps, live kernel reports). Needs elevation.</summary>
public sealed class SystemMemoryDumpCleaner : WindowsCleanerBase
{
    public override string Id => "memory-dumps";

    public override string Name => "System memory dumps";

    public override bool RequiresElevation => true;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var windows = context.Environment.WindowsDirectory;
        if (windows is not null)
        {
            yield return new CleanupPath(Path.Combine(windows, "Minidump"), DeleteMode.ClearContents, "minidumps");
            yield return new CleanupPath(Path.Combine(windows, "LiveKernelReports"), DeleteMode.ClearContents, "live kernel reports");
        }
    }
}

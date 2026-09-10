using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Services;

namespace Cleaner.Core.Cleaners.Os;

/// <summary>Application crash dumps and Windows Error Reporting queues.</summary>
public sealed class CrashDumpCleaner : WindowsCleanerBase
{
    public override string Id => "crash-dumps";

    public override string Name => "Crash dumps & error reports";

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var local = context.Environment.LocalAppDataDirectory;
        yield return new CleanupPath(Path.Combine(local, "CrashDumps"), DeleteMode.ClearContents, "crash dumps");
        yield return new CleanupPath(Path.Combine(local, "Microsoft", "Windows", "WER"), DeleteMode.ClearContents, "error reports");
    }
}

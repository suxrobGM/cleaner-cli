using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.Os;

/// <summary>
/// AMD usage-telemetry logs under <c>ProgramData\AMD\PPC</c>. The driver recreates these logs;
/// configuration files are preserved.
/// </summary>
public sealed class AmdTelemetryCleaner : WindowsCleanerBase
{
    private static readonly string[] LogFiles = ["sdkusage.csv", "apprecord.csv", "driverworkloadstats.csv"];

    /// <summary>Staging folders for reports queued to upload; all transient.</summary>
    private static readonly string[] StagingDirectories = ["compress", "send", "upload", "temp"];

    public override string Id => "amd-telemetry";

    public override string Name => "AMD driver telemetry logs";

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var root = OsPaths.ProgramData(context.Environment, "AMD", "PPC");
        if (root is null)
        {
            yield break;
        }

        foreach (var file in LogFiles)
        {
            yield return new CleanupPath(Path.Combine(root, file), DeleteMode.DeleteFile, file);
        }

        foreach (var directory in StagingDirectories)
        {
            yield return new CleanupPath(Path.Combine(root, directory), DeleteMode.ClearContents, directory);
        }
    }
}

using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.Os;

/// <summary>
/// AMD's usage-telemetry logs under <c>ProgramData\AMD\PPC</c>. They are append-only and never
/// rotated, so <c>sdkusage.csv</c> alone reaches several GB on a machine with AMD drivers. The
/// driver recreates them on demand; <c>config.csv</c> is left alone.
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
        var root = ProgramDataPath(context, "AMD", "PPC");
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

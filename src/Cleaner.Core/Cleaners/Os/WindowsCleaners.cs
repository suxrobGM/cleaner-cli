using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.Os;

/// <summary>Base for Windows-only cleaners.</summary>
public abstract class WindowsCleanerBase : DirectoryCleanerBase
{
    public override string Category => Categories.OperatingSystem;

    public override bool IsApplicable(CleanupContext context) => context.Environment.IsWindows;
}

/// <summary>Downloaded Windows Update payloads (SoftwareDistribution\Download). Needs elevation.</summary>
public sealed class WindowsUpdateCacheCleaner : WindowsCleanerBase
{
    public override string Id => "windows-update";

    public override string Name => "Windows Update cache";

    public override bool RequiresElevation => true;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var windows = context.Environment.WindowsDirectory;
        if (windows is not null)
        {
            yield return new CleanupPath(Path.Combine(windows, "SoftwareDistribution", "Download"), DeleteMode.ClearContents);
        }
    }
}

/// <summary>The machine-wide Windows\Temp directory. Needs elevation.</summary>
public sealed class WindowsTempCleaner : WindowsCleanerBase
{
    public override string Id => "windows-temp";

    public override string Name => "Windows temp (system)";

    public override bool RequiresElevation => true;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var windows = context.Environment.WindowsDirectory;
        if (windows is not null)
        {
            yield return new CleanupPath(Path.Combine(windows, "Temp"), DeleteMode.ClearContents);
        }
    }
}

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

/// <summary>Windows Delivery Optimization download cache. Needs elevation.</summary>
public sealed class DeliveryOptimizationCleaner : WindowsCleanerBase
{
    public override string Id => "delivery-optimization";

    public override string Name => "Delivery Optimization cache";

    public override bool RequiresElevation => true;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var windows = context.Environment.WindowsDirectory;
        if (windows is null)
        {
            yield break;
        }

        var root = Path.GetPathRoot(windows) ?? "C:\\";
        yield return new CleanupPath(
            Path.Combine(root, "Windows", "ServiceProfiles", "NetworkService", "AppData", "Local",
                "Microsoft", "Windows", "DeliveryOptimization", "Cache"),
            DeleteMode.ClearContents);
    }
}

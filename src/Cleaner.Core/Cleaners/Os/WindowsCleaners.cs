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

/// <summary>Windows servicing and component logs (CBS, DISM, WindowsUpdate). Needs elevation.</summary>
public sealed class WindowsLogCleaner : WindowsCleanerBase
{
    public override string Id => "windows-logs";

    public override string Name => "Windows system logs";

    public override bool RequiresElevation => true;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var windows = context.Environment.WindowsDirectory;
        if (windows is not null)
        {
            yield return new CleanupPath(Path.Combine(windows, "Logs"), DeleteMode.ClearContents);
        }
    }
}

/// <summary>Temp directories of the built-in service accounts (LocalService, NetworkService). Needs elevation.</summary>
public sealed class ServiceProfileTempCleaner : WindowsCleanerBase
{
    private static readonly string[] ServiceAccounts = ["LocalService", "NetworkService"];

    public override string Id => "service-temp";

    public override string Name => "Service account temp";

    public override bool RequiresElevation => true;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var windows = context.Environment.WindowsDirectory;
        if (windows is null)
        {
            yield break;
        }

        foreach (var account in ServiceAccounts)
        {
            yield return new CleanupPath(
                Path.Combine(windows, "ServiceProfiles", account, "AppData", "Local", "Temp"),
                DeleteMode.ClearContents,
                account);
        }
    }
}

/// <summary>Leftover downloaded installers and ActiveX/Java applet caches. Needs elevation.</summary>
public sealed class DownloadedProgramFilesCleaner : WindowsCleanerBase
{
    public override string Id => "downloaded-program-files";

    public override string Name => "Downloaded program files";

    public override bool RequiresElevation => true;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var windows = context.Environment.WindowsDirectory;
        if (windows is not null)
        {
            yield return new CleanupPath(Path.Combine(windows, "Downloaded Program Files"), DeleteMode.ClearContents, "ActiveX/Java applets");
            yield return new CleanupPath(Path.Combine(windows, "Downloaded Installations"), DeleteMode.ClearContents, "installers");
        }
    }
}

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

/// <summary>GPU driver shader caches (DirectX, NVIDIA, AMD, Intel). All re-compile on demand.</summary>
public sealed class GpuShaderCacheCleaner : WindowsCleanerBase
{
    public override string Id => "gpu-shader-cache";

    public override string Name => "GPU shader caches";

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var local = context.Environment.LocalAppDataDirectory;
        yield return new CleanupPath(Path.Combine(local, "D3DSCache"), DeleteMode.ClearContents, "DirectX");
        yield return new CleanupPath(Path.Combine(local, "NVIDIA", "DXCache"), DeleteMode.ClearContents, "NVIDIA DX");
        yield return new CleanupPath(Path.Combine(local, "NVIDIA", "GLCache"), DeleteMode.ClearContents, "NVIDIA GL");
        yield return new CleanupPath(Path.Combine(local, "NVIDIA", "NV_Cache"), DeleteMode.ClearContents, "NVIDIA");
        yield return new CleanupPath(Path.Combine(local, "AMD", "DxCache"), DeleteMode.ClearContents, "AMD DX");
        yield return new CleanupPath(Path.Combine(local, "AMD", "DxcCache"), DeleteMode.ClearContents, "AMD DXC");
        yield return new CleanupPath(Path.Combine(local, "Intel", "ShaderCache"), DeleteMode.ClearContents, "Intel");
    }
}

/// <summary>WinINet / "Temporary Internet Files" cache used by IE, legacy Edge, and WebView hosts.</summary>
public sealed class InetCacheCleaner : WindowsCleanerBase
{
    public override string Id => "inet-cache";

    public override string Name => "Temporary Internet Files";

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        yield return new CleanupPath(
            Path.Combine(context.Environment.LocalAppDataDirectory, "Microsoft", "Windows", "INetCache"),
            DeleteMode.ClearContents);
    }
}

/// <summary>
/// Per-package caches and scratch state of Microsoft Store / UWP apps. Only clears the well-known
/// cache subdirectories under each package — never <c>LocalState</c> or <c>Settings</c> (real data).
/// </summary>
public sealed class StoreAppCacheCleaner : WindowsCleanerBase
{
    private static readonly string[] CacheSubdirectories =
    [
        Path.Combine("AC", "INetCache"),
        Path.Combine("AC", "Temp"),
        "TempState",
    ];

    public override string Id => "store-app-cache";

    public override string Name => "Store app caches";

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var packages = Path.Combine(context.Environment.LocalAppDataDirectory, "Packages");
        foreach (var package in context.FileSystem.EnumerateDirectories(packages))
        {
            foreach (var sub in CacheSubdirectories)
            {
                yield return new CleanupPath(Path.Combine(package, sub), DeleteMode.ClearContents);
            }
        }
    }
}

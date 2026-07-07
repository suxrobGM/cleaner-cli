using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Services;

namespace Cleaner.Core.Cleaners.Os;

/// <summary>
/// GPU driver installer leftovers: the extraction folders NVIDIA/AMD/Intel installers drop at the
/// drive root and NVIDIA's download cache. Never touches DriverStore, Installer2 (needed for driver
/// repair/uninstall), or any installed driver files. Shader caches are covered by gpu-shader-cache.
/// </summary>
public sealed class GpuInstallerLeftoverCleaner : WindowsCleanerBase
{
    public override string Id => "gpu-installers";

    public override string Name => "GPU driver installer leftovers";

    public override bool RequiresElevation => true;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        var windows = env.WindowsDirectory;
        if (windows is null)
        {
            yield break;
        }

        yield return new CleanupPath(
            OsPaths.FromWindowsDriveRoot(windows, "NVIDIA"), Description: "NVIDIA installer extraction");
        yield return new CleanupPath(
            OsPaths.FromWindowsDriveRoot(windows, "AMD"), Description: "AMD installer extraction");
        yield return new CleanupPath(
            OsPaths.FromWindowsDriveRoot(windows, "Intel"), Description: "Intel installer extraction");

        var programData = OsPaths.Env(env, "ProgramData") ?? OsPaths.FromWindowsDriveRoot(windows, "ProgramData");
        yield return new CleanupPath(
            Path.Combine(programData, "NVIDIA Corporation", "Downloader"),
            DeleteMode.ClearContents,
            "NVIDIA driver downloads");
    }
}

using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Services;

namespace Cleaner.Core.Cleaners.Os;

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

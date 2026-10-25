using Cleaner.Core.Abstractions;

namespace Cleaner.Core.Cleaners.Base;

/// <summary>
/// Cache subdirectories shared by Chromium browsers and Electron apps. Cookies, history, passwords,
/// storage, and IndexedDB are user data and are excluded.
/// </summary>
internal static class ChromiumCache
{
    public static readonly string[] Directories =
    [
        "Cache",
        "Code Cache",
        "GPUCache",
        "DawnCache",
        "DawnGraphiteCache",
        "DawnWebGPUCache",
        "GrShaderCache",
        "ShaderCache",
    ];

    /// <summary>On-device model stores beside browser profiles; Chrome and Edge re-download them as needed.</summary>
    public static readonly string[] ModelStores =
        ["OptGuideOnDeviceModel", "OptGuideOnDeviceClassifierModel", "optimization_guide_model_store"];

    /// <summary>Each cache directory directly under <paramref name="root"/>, cleared in place.</summary>
    public static IEnumerable<CleanupPath> Under(string root, string? description = null)
    {
        foreach (var sub in Directories)
        {
            yield return new CleanupPath(Path.Combine(root, sub), DeleteMode.ClearContents, description);
        }

        // Service Worker CacheStorage holds fetched assets (the Cache API), not site state to keep.
        yield return new CleanupPath(
            Path.Combine(root, "Service Worker", "CacheStorage"), DeleteMode.ClearContents, description);
    }

    public static IEnumerable<CleanupPath> ModelStoresUnder(string userDataRoot, string description)
    {
        foreach (var store in ModelStores)
        {
            yield return new CleanupPath(
                Path.Combine(userDataRoot, store), DeleteMode.ClearContents, $"{description} on-device models");
        }
    }
}

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

    /// <summary>
    /// On-device model stores beside browser profiles; Chrome and Edge re-download them as needed.
    /// </summary>
    public static readonly string[] ModelStores =
        ["OptGuideOnDeviceModel", "OptGuideOnDeviceClassifierModel", "optimization_guide_model_store"];

    /// <summary>Yields each cache directory directly under <paramref name="root"/>, cleared in place.</summary>
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

    /// <summary>
    /// Yields the model stores under a browser's User Data root; the browser re-downloads whatever
    /// it still needs.
    /// </summary>
    public static IEnumerable<CleanupPath> ModelStoresUnder(string userDataRoot, string description)
    {
        foreach (var store in ModelStores)
        {
            yield return new CleanupPath(
                Path.Combine(userDataRoot, store), DeleteMode.ClearContents, $"{description} on-device models");
        }
    }
}

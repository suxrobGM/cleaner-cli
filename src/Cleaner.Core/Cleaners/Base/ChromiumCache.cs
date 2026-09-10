namespace Cleaner.Core.Cleaners.Base;

/// <summary>
/// The cache subdirectory names every Chromium app shares, browsers and Electron apps alike, so
/// their cleaners don't each repeat the list. Only re-fetchable data: cookies, history, passwords,
/// Local/Session Storage, and IndexedDB are user data and stay out.
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
}

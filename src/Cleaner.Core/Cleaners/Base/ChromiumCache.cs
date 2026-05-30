namespace Cleaner.Core.Cleaners.Base;

/// <summary>
/// Every Chromium-based app — desktop browsers and Electron apps alike — stores its HTTP, GPU, and
/// shader caches under the same well-known subdirectory names. This centralizes those names and the
/// "clear the cache dirs under a root" shape so browser and app cleaners don't duplicate it.
/// </summary>
/// <remarks>
/// Only re-fetchable or re-derived caches are listed. Cookies, history, passwords, "Local Storage",
/// "Session Storage", and IndexedDB are deliberately excluded — those are user data.
/// </remarks>
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

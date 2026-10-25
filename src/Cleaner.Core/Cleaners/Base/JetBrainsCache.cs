namespace Cleaner.Core.Cleaners.Base;

/// <summary>
/// Derived-data subdirectories shared by JetBrains IDEs. Other product-directory contents may be
/// settings, plugins, or local history and are not targeted.
/// </summary>
internal static class JetBrainsCache
{
    public static readonly string[] Directories = ["caches", "index", "log", "tmp"];

    /// <summary>The shared derived-data folders plus any <paramref name="additional"/> product-specific ones.</summary>
    public static IEnumerable<CleanupPath> Under(string productDir, string product, params string[] additional)
    {
        foreach (var sub in Directories.Concat(additional))
        {
            yield return new CleanupPath(Path.Combine(productDir, sub), Description: $"{product} {sub}");
        }
    }
}

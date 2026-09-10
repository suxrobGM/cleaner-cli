namespace Cleaner.Core.Cleaners.Base;

/// <summary>
/// The derived-data subdirectory names of a JetBrains product directory, shared by every IDE built
/// on the platform (IntelliJ, Rider, and Android Studio under its own Google root) so their
/// cleaners don't each repeat the list. Everything else in a product directory may be settings,
/// installed plugins, or local history.
/// </summary>
internal static class JetBrainsCache
{
    public static readonly string[] Directories = ["caches", "index", "log", "tmp"];

    /// <summary>
    /// Yields each derived-data directory under <paramref name="productDir"/>, labelled with
    /// <paramref name="product"/>. <paramref name="additional"/> covers names only some IDEs use.
    /// </summary>
    public static IEnumerable<CleanupPath> Under(string productDir, string product, params string[] additional)
    {
        foreach (var sub in Directories.Concat(additional))
        {
            yield return new CleanupPath(Path.Combine(productDir, sub), Description: $"{product} {sub}");
        }
    }
}

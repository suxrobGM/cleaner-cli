using Cleaner.Core.Services;
using Cleaner.Core.Utils;

namespace Cleaner.Core.Cleaners.Base;

/// <summary>
/// Traversal for workspace sweep cleaners; matching directories are yielded and not traversed.
/// </summary>
internal static class DirectorySweep
{
    /// <summary>The final path segment (directory name) of <paramref name="path"/>.</summary>
    public static string LeafName(string path) => PathComparison.Leaf(path);

    /// <summary>
    /// Depth-first walk below each existing root. A child is tested with <paramref name="isMatch"/>
    /// first and yielded on a match; otherwise it is entered only if <paramref name="canDescend"/>
    /// allows. Roots themselves are never yielded, and links are never followed (see
    /// <see cref="IFileSystemService.EnumerateDirectories"/>).
    /// </summary>
    public static IEnumerable<string> FindDirectories(
        IFileSystemService fileSystem,
        IEnumerable<string> roots,
        Func<string, bool> isMatch,
        Func<string, bool> canDescend)
    {
        foreach (var root in roots)
        {
            if (string.IsNullOrWhiteSpace(root) || !fileSystem.DirectoryExists(root))
            {
                continue;
            }

            var stack = new Stack<string>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                foreach (var child in fileSystem.EnumerateDirectories(stack.Pop()))
                {
                    if (isMatch(child))
                    {
                        yield return child;
                    }
                    else if (canDescend(child))
                    {
                        stack.Push(child);
                    }
                }
            }
        }
    }
}

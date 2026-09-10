using Cleaner.Core.Services;

namespace Cleaner.Core.Cleaners.Base;

/// <summary>
/// Traversal for workspace sweep cleaners; matching directories are yielded and not traversed.
/// </summary>
internal static class DirectorySweep
{
    private static readonly char[] Separators = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];

    /// <summary>The final path segment (directory name) of <paramref name="path"/>.</summary>
    public static string LeafName(string path) => Path.GetFileName(path.TrimEnd(Separators));

    /// <summary>
    /// Depth-first walk of existing roots, optionally pruning branches with
    /// <paramref name="skipDescentInto"/>.
    /// </summary>
    public static IEnumerable<string> FindDirectories(
        IFileSystemService fileSystem,
        IEnumerable<string> roots,
        Func<string, bool> isMatch,
        Func<string, bool>? skipDescentInto = null)
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
                var current = stack.Pop();
                if (isMatch(current))
                {
                    yield return current;
                    continue;
                }

                foreach (var child in fileSystem.EnumerateDirectories(current))
                {
                    if (skipDescentInto is null || !skipDescentInto(child))
                    {
                        stack.Push(child);
                    }
                }
            }
        }
    }
}

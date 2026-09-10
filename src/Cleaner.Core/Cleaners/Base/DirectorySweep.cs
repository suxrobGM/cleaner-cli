using Cleaner.Core.Services;

namespace Cleaner.Core.Cleaners.Base;

/// <summary>
/// Traversal for workspace-sweeping cleaners: walk the scan roots, yield each directory the
/// predicate matches, and never descend into a match.
/// </summary>
internal static class DirectorySweep
{
    private static readonly char[] Separators = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];

    /// <summary>The final path segment (directory name) of <paramref name="path"/>.</summary>
    public static string LeafName(string path) => Path.GetFileName(path.TrimEnd(Separators));

    /// <summary>
    /// Depth-first walk of every existing root, yielding each match without descending into it.
    /// <paramref name="skipDescentInto"/> prunes whole branches from the walk.
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

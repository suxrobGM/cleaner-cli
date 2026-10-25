using Cleaner.Core.Services;
using Cleaner.Core.Utils;

namespace Cleaner.Core.Cleaners.Base;

/// <summary>Finds matching directories below a set of folders, without descending into matches.</summary>
internal static class DirectorySearch
{
    /// <summary>
    /// Depth-first walk below each existing root. A matching child is yielded and not entered; any other
    /// child is entered only if <paramref name="canEnter"/> allows. Roots are never yielded, and links are
    /// never followed.
    /// </summary>
    public static IEnumerable<string> Find(
        IFileSystemService fileSystem,
        IEnumerable<string> roots,
        Func<string, bool> isMatch,
        Func<string, bool> canEnter)
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
                    else if (canEnter(child))
                    {
                        stack.Push(child);
                    }
                }
            }
        }
    }
}

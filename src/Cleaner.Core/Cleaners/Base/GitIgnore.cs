using Cleaner.Core.Services;
using Cleaner.Core.Utils;

namespace Cleaner.Core.Cleaners.Base;

/// <summary>
/// Just enough <c>.gitignore</c> reading to tell whether a project ignores a folder by name, which
/// is the project's own statement that the folder is generated.
/// </summary>
internal static class GitIgnore
{
    /// <summary>How far up to look for <c>.gitignore</c> files before giving up on finding the repo root.</summary>
    private const int MaxLevels = 16;

    /// <summary>
    /// True when a <c>.gitignore</c> in <paramref name="projectDirectory"/> or any parent up to the
    /// repository root ignores a direct child named <paramref name="name"/>. Only single-segment
    /// patterns count (<c>dist</c>, <c>dist/</c>, <c>/dist</c>, <c>**/dist</c>, <c>[Bb]uild/</c>);
    /// a negation of the name anywhere means "not ignored".
    /// </summary>
    public static bool ListsDirectory(IFileSystemService fs, string projectDirectory, string name)
    {
        var listed = false;
        var directory = projectDirectory;
        for (var level = 0; directory is not null && level < MaxLevels; level++)
        {
            foreach (var raw in fs.ReadAllLines(Path.Combine(directory, ".gitignore")))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith('#'))
                {
                    continue;
                }

                var negated = line.StartsWith('!');
                if (Matches(negated ? line[1..] : line, name, anchoredHere: level == 0))
                {
                    if (negated)
                    {
                        return false;
                    }

                    listed = true;
                }
            }

            var git = Path.Combine(directory, ".git");
            if (fs.DirectoryExists(git) || fs.FileExists(git))
            {
                break;
            }

            directory = PathComparison.Parent(directory);
        }

        return listed;
    }

    /// <param name="anchoredHere">
    /// Whether the .gitignore sits in the folder's own parent. A leading <c>/</c> anchors a pattern to
    /// its .gitignore's folder, so <c>/dist</c> higher up doesn't speak for this project's dist.
    /// </param>
    private static bool Matches(string pattern, string name, bool anchoredHere)
    {
        var segment = pattern;
        if (segment.StartsWith("**/", StringComparison.Ordinal))
        {
            segment = segment[3..];
        }
        else if (segment.StartsWith('/'))
        {
            if (!anchoredHere)
            {
                return false;
            }

            segment = segment[1..];
        }

        segment = segment.TrimEnd('/');
        return segment.Length > 0 && !segment.Contains('/') && Glob(segment, 0, name, 0);
    }

    /// <summary>Case-insensitive <c>*</c>, <c>?</c>, and <c>[...]</c> matching of one path segment.</summary>
    private static bool Glob(string pattern, int p, string text, int t)
    {
        while (p < pattern.Length)
        {
            switch (pattern[p])
            {
                case '*':
                    for (var skip = t; skip <= text.Length; skip++)
                    {
                        if (Glob(pattern, p + 1, text, skip))
                        {
                            return true;
                        }
                    }

                    return false;

                case '?':
                    if (t >= text.Length)
                    {
                        return false;
                    }

                    p++;
                    t++;
                    break;

                case '[':
                    var close = pattern.IndexOf(']', p + 1);
                    if (close < 0 || t >= text.Length)
                    {
                        return false;
                    }

                    var set = pattern[(p + 1)..close];
                    if (!set.Contains(text[t], StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }

                    p = close + 1;
                    t++;
                    break;

                default:
                    if (t >= text.Length || char.ToLowerInvariant(pattern[p]) != char.ToLowerInvariant(text[t]))
                    {
                        return false;
                    }

                    p++;
                    t++;
                    break;
            }
        }

        return t == text.Length;
    }
}

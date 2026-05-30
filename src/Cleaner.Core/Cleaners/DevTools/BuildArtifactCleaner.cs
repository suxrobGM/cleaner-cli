using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// Sweeps common build-output and dependency directories under the working directory
/// (<c>--path</c>, default cwd): bin, obj, node_modules, target, dist, .next, .gradle. Matched
/// directories are not descended into. Opt-in — only acts on the path you point it at.
/// </summary>
public sealed class BuildArtifactCleaner : DirectoryCleanerBase
{
    private static readonly HashSet<string> ArtifactNames =
        new(StringComparer.OrdinalIgnoreCase) { "bin", "obj", "node_modules", "target", "dist", ".next", ".gradle" };

    public override string Id => "build-artifacts";

    public override string Name => "Project build artifacts";

    public override string Category => Categories.ProjectLocal;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var root = context.WorkingDirectory;
        if (!context.FileSystem.DirectoryExists(root))
        {
            yield break;
        }

        var separators = new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
        var stack = new Stack<string>();
        stack.Push(root);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            foreach (var child in context.FileSystem.EnumerateDirectories(current))
            {
                var name = Path.GetFileName(child.TrimEnd(separators));
                if (ArtifactNames.Contains(name))
                {
                    // Match found — collect it and don't descend further into it.
                    yield return new CleanupPath(child, Description: name);
                }
                else
                {
                    stack.Push(child);
                }
            }
        }
    }
}

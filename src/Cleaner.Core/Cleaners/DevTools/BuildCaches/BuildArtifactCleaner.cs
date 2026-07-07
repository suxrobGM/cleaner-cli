using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// Sweeps common build-output and dependency directories under each scan root (<c>--path</c>,
/// repeatable; default cwd): bin, obj, node_modules, target, dist, framework build outputs, and
/// Python/Terragrunt tool caches. Matched directories are not descended into. Opt-in — only acts on
/// the roots you point it at, so it can reclaim a whole workspace (e.g. <c>--path ~/source</c>) in
/// one pass.
/// </summary>
public sealed class BuildArtifactCleaner : DirectoryCleanerBase
{
    private static readonly HashSet<string> ArtifactNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", "node_modules", "target", "dist", ".next", ".gradle",
        ".nuxt", ".svelte-kit", ".astro",
        "__pycache__", ".pytest_cache", ".mypy_cache", ".ruff_cache",
        ".terragrunt-cache",
    };

    public override string Id => "build-artifacts";

    public override string Name => "Project build artifacts";

    public override string Category => Categories.ProjectLocal;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        DirectorySweep
            .FindDirectories(context.FileSystem, context.ScanRoots, dir => ArtifactNames.Contains(DirectorySweep.LeafName(dir)))
            .Select(dir => new CleanupPath(dir, Description: DirectorySweep.LeafName(dir)));
}

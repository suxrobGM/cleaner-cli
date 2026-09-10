using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Services;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// Sweeps build-output and dependency directories under each scan root (<c>--path</c>, repeatable;
/// default cwd): bin, obj, node_modules, target, dist, framework build outputs, Python virtualenvs
/// and tool caches. Matched directories are not descended into. Opt-in — it only acts on the roots
/// you point it at, so it can reclaim a whole workspace (e.g. <c>--path ~/source</c>) in one pass.
/// </summary>
public sealed class BuildArtifactCleaner : DirectoryCleanerBase
{
    private static readonly HashSet<string> ArtifactNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", "node_modules", "target", "dist", ".next", ".gradle",
        ".nuxt", ".svelte-kit", ".astro", ".turbo", ".parcel-cache", ".vite",
        "__pycache__", ".pytest_cache", ".mypy_cache", ".ruff_cache",
        ".venv", "venv", ".tox", ".nox",
        ".terragrunt-cache",
    };

    /// <summary>
    /// Marks a directory as a project whose build system writes to <c>build/</c>. That name is too
    /// common to sweep on sight, so it is only taken when one of these sits beside it.
    /// </summary>
    private static readonly string[] BuildSystemMarkers =
    [
        "build.gradle", "build.gradle.kts", "settings.gradle", "settings.gradle.kts",
        "gradlew", "pom.xml", "CMakeLists.txt", "meson.build",
    ];

    public override string Id => "build-artifacts";

    public override string Name => "Project build artifacts";

    public override string Category => Categories.ProjectLocal;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        DirectorySweep
            .FindDirectories(context.FileSystem, context.ScanRoots, dir => IsArtifact(context.FileSystem, dir))
            .Select(dir => new CleanupPath(dir, Description: DirectorySweep.LeafName(dir)));

    private static bool IsArtifact(IFileSystemService fileSystem, string directory)
    {
        var name = DirectorySweep.LeafName(directory);
        return ArtifactNames.Contains(name)
            || (name.Equals("build", StringComparison.OrdinalIgnoreCase) && HasBuildSystemSibling(fileSystem, directory));
    }

    private static bool HasBuildSystemSibling(IFileSystemService fileSystem, string directory)
    {
        var parent = Path.GetDirectoryName(directory);
        return parent is { Length: > 0 }
            && BuildSystemMarkers.Any(marker => fileSystem.FileExists(Path.Combine(parent, marker)));
    }
}

using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>Conda package cache. Uses <c>conda clean --all --yes</c>.</summary>
public sealed class CondaCleaner : ProcessCleanerBase
{
    public override string Id => "conda";

    public override string Name => "Conda package cache";

    public override string Category => Categories.Python;

    protected override string Executable => "conda";

    protected override IReadOnlyList<string> CleanArguments => ["clean", "--all", "--yes"];

    /// <summary>Conventional install roots whose <c>pkgs</c> dir holds the package cache.</summary>
    private static readonly string[] InstallRoots = ["miniconda3", "anaconda3", "miniforge3", "mambaforge"];

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;

        // Explicit cache relocation: CONDA_PKGS_DIRS is a comma-separated list of cache dirs.
        if (OsPaths.Env(env, "CONDA_PKGS_DIRS") is { } pkgsDirs)
        {
            foreach (var dir in pkgsDirs.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                yield return new CleanupPath(dir, Description: "package cache");
            }
        }

        // The cache normally lives under the install root (<root>/pkgs), not ~/.conda.
        foreach (var name in new[] { "CONDA_PREFIX", "CONDA_ROOT", "MAMBA_ROOT_PREFIX" })
        {
            if (OsPaths.Env(env, name) is { } root)
            {
                yield return new CleanupPath(Path.Combine(root, "pkgs"), Description: "package cache");
            }
        }

        foreach (var root in InstallRoots)
        {
            yield return new CleanupPath(env.HomePath(root, "pkgs"), Description: "package cache");
        }

        // Legacy per-user location, kept for completeness.
        yield return new CleanupPath(env.HomePath(".conda", "pkgs"), Description: "package cache");
    }
}

using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>pip download/wheel cache. Prefers <c>pip cache purge</c>.</summary>
public sealed class PipCleaner : ProcessCleanerBase
{
    public override string Id => "pip";

    public override string Name => "pip cache";

    public override string Category => Categories.Python;

    protected override string Executable => "pip";

    protected override IReadOnlyList<string> CleanArguments => ["cache", "purge"];

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return new CleanupPath(
            OsPaths.Env(env, "PIP_CACHE_DIR") ?? OsPaths.AppCache(env, Path.Combine("pip", "Cache"), "pip", "pip"));
    }
}

/// <summary>pipenv virtualenv/cache directory.</summary>
public sealed class PipenvCleaner : DirectoryCleanerBase
{
    public override string Id => "pipenv";

    public override string Name => "pipenv cache";

    public override string Category => Categories.Python;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        [new CleanupPath(OsPaths.AppCache(context.Environment, Path.Combine("pipenv", "Cache"), "pipenv", "pipenv"))];
}

/// <summary>Poetry artifact and download cache.</summary>
public sealed class PoetryCleaner : DirectoryCleanerBase
{
    public override string Id => "poetry";

    public override string Name => "Poetry cache";

    public override string Category => Categories.Python;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return new CleanupPath(
            OsPaths.Env(env, "POETRY_CACHE_DIR")
            ?? OsPaths.AppCache(env, Path.Combine("pypoetry", "Cache"), "pypoetry", "pypoetry"));
    }
}

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

/// <summary>PDM package/wheel cache. Prefers <c>pdm cache clear</c>.</summary>
public sealed class PdmCleaner : ProcessCleanerBase
{
    public override string Id => "pdm";

    public override string Name => "PDM cache";

    public override string Category => Categories.Python;

    protected override string Executable => "pdm";

    protected override IReadOnlyList<string> CleanArguments => ["cache", "clear"];

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        [new CleanupPath(OsPaths.AppCache(context.Environment, Path.Combine("pdm", "Cache"), "pdm", "pdm"))];
}

/// <summary>uv (Astral) download and build cache.</summary>
public sealed class UvCleaner : DirectoryCleanerBase
{
    public override string Id => "uv";

    public override string Name => "uv cache";

    public override string Category => Categories.Python;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return new CleanupPath(
            OsPaths.Env(env, "UV_CACHE_DIR") ?? OsPaths.AppCache(env, Path.Combine("uv", "cache"), "uv", "uv"));
    }
}

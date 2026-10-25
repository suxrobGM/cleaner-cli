using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>Conda package cache. Prefers <c>conda clean --all --yes</c>.</summary>
/// <remarks>
/// Without conda, only the archives inside each <c>pkgs</c> dir are deleted: the extracted folders may be
/// hard-linked into environments, and only conda knows which are unused.
/// </remarks>
public sealed class CondaCleaner : ProcessCleanerBase
{
    public override string Id => "conda";

    public override string Name => "Conda package cache";

    public override string Category => Categories.Python;

    protected override string Executable => "conda";

    protected override IReadOnlyList<string> CleanArguments => ["clean", "--all", "--yes"];

    /// <summary>Downloaded archives only; extracted folders are never matched.</summary>
    private static readonly string[] ArchiveExtensions = [".conda", ".tar.bz2"];

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        foreach (var dir in PackageDirectories(context))
        {
            if (!context.FileSystem.DirectoryExists(dir))
            {
                continue;
            }

            foreach (var file in context.FileSystem.EnumerateFiles(dir))
            {
                if (ArchiveExtensions.Any(ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                {
                    yield return new CleanupPath(file, DeleteMode.DeleteFile, "package archive");
                }
            }
        }
    }

    private static IEnumerable<string> PackageDirectories(CleanupContext context)
    {
        var env = context.Environment;

        // CONDA_PKGS_DIRS is a comma-separated list.
        if (OsPaths.Env(env, "CONDA_PKGS_DIRS") is { } pkgsDirs)
        {
            foreach (var dir in pkgsDirs.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                yield return dir;
            }
        }

        // The cache normally lives under the install root (<root>/pkgs).
        if (OsPaths.Env(env, "CONDA_PREFIX") is { } prefix)
        {
            yield return Path.Combine(prefix, "pkgs");
        }

        foreach (var root in OsPaths.CondaRoots(env))
        {
            yield return Path.Combine(root, "pkgs");
        }
    }
}

using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Services;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// Unity's global editor cache plus the regenerable per-project <c>Library</c>/<c>Temp</c>/<c>Logs</c>/
/// <c>obj</c> folders — only inside actual Unity projects (a dir with both <c>Assets</c> and
/// <c>ProjectSettings</c>) found under the scan roots (<c>--path</c>, repeatable; default cwd), so the
/// generic names are never deleted elsewhere. Unity rebuilds these; player builds and the Asset Store
/// cache are left alone.
/// </summary>
public sealed class UnityCleaner : DirectoryCleanerBase
{
    private static readonly string[] ProjectArtifacts = ["Library", "Temp", "Logs", "obj"];

    public override string Id => "unity";

    public override string Name => "Unity caches & project artifacts";

    public override string Category => Categories.GameDev;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        GlobalCaches(context).Concat(ProjectCaches(context));

    private static IEnumerable<CleanupPath> GlobalCaches(CleanupContext context)
    {
        var env = context.Environment;

        if (env.IsWindows)
        {
            yield return new CleanupPath(
                Path.Combine(env.LocalAppDataDirectory, "Unity", "cache"),
                DeleteMode.ClearContents, "editor cache");
            yield break;
        }

        if (env.IsMacOs)
        {
            yield return new CleanupPath(
                Path.Combine(env.HomeDirectory, "Library", "Unity", "cache"),
                DeleteMode.ClearContents, "editor cache");
            yield break;
        }

        yield return new CleanupPath(Path.Combine(env.CacheDirectory, "unity3d"), DeleteMode.ClearContents, "editor cache");
    }

    private static IEnumerable<CleanupPath> ProjectCaches(CleanupContext context)
    {
        var fs = context.FileSystem;

        // Find Unity projects under the scan roots, skipping descent into the regenerable artifact
        // folders themselves so a stray bin/obj/Library elsewhere doesn't get walked needlessly.
        var projects = DirectorySweep.FindDirectories(
            fs,
            context.ScanRoots,
            dir => IsUnityProject(fs, dir),
            skipDescentInto: dir => ProjectArtifacts.Contains(DirectorySweep.LeafName(dir), StringComparer.OrdinalIgnoreCase));

        return projects.SelectMany(project =>
            ProjectArtifacts.Select(artifact => new CleanupPath(Path.Combine(project, artifact), Description: artifact)));
    }

    private static bool IsUnityProject(IFileSystemService fs, string dir) =>
        fs.DirectoryExists(Path.Combine(dir, "Assets")) &&
        fs.DirectoryExists(Path.Combine(dir, "ProjectSettings"));
}

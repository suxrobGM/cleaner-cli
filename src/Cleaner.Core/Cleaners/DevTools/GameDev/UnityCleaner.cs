using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Services;
using Cleaner.Core.Utils;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// Unity's global editor cache and regenerable <c>Library</c>/<c>Temp</c>/<c>Logs</c>/<c>obj</c>
/// folders inside detected Unity projects. Scan roots come from repeatable <c>--path</c> options
/// (defaulting to the current directory); player builds and the Asset Store cache are preserved.
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
        var roots = ProjectRoots.Allowed(context);

        // Find Unity projects under the scan roots (a root may itself be one), never entering the
        // regenerable artifact folders, links, dot-folders, or installed tools along the way.
        var projects = roots
            .Where(root => IsUnityProject(fs, root))
            .Concat(DirectorySearch.Find(
                fs,
                roots,
                dir => IsUnityProject(fs, dir),
                dir => context.Guard.CanEnter(dir)
                    && !ProjectArtifacts.Contains(PathComparison.Name(dir), StringComparer.OrdinalIgnoreCase)));

        return projects.SelectMany(project =>
            ProjectArtifacts.Select(artifact => new CleanupPath(Path.Combine(project, artifact), Description: artifact)));
    }

    protected override IEnumerable<string> ScanNotes(CleanupContext context) => ProjectRoots.Refusals(context);

    /// <summary>ProjectVersion.txt is what Unity writes; Assets plus ProjectSettings alone could be anything.</summary>
    private static bool IsUnityProject(IFileSystemService fs, string dir) =>
        fs.DirectoryExists(Path.Combine(dir, "Assets")) &&
        fs.FileExists(Path.Combine(dir, "ProjectSettings", "ProjectVersion.txt"));
}

using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// Sweeps build outputs, dependency directories, virtualenvs, and tool caches inside projects under
/// the scan roots (<c>--path</c>, repeatable; default current directory).
/// </summary>
/// <remarks>
/// A folder is taken only when <see cref="ArtifactRules"/> proves it is a project's generated
/// output: a <c>bin</c> needs a .NET project and a restore beside it, a <c>node_modules</c> needs a
/// package.json and a lockfile, and so on. The sweep refuses to start from the profile, a drive
/// root, or a system folder, never enters links, dot-folders, or installed tools, and is opt-in so
/// "All cleaners" never includes it.
/// </remarks>
public sealed class BuildArtifactCleaner : DirectoryCleanerBase
{
    public override string Id => "build-artifacts";

    public override string Name => "Project build artifacts";

    public override string Category => Categories.ProjectLocal;

    public override bool OptIn => true;

    public override string ConfirmationWarning =>
        "removes build output and dependencies (bin/obj, node_modules, target, dist, virtualenvs) from " +
        "the projects under the scanned folders; each project needs a rebuild or reinstall afterwards";

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        DirectorySweep
            .FindDirectories(
                context.FileSystem,
                WorkspaceRoots.Usable(context),
                dir => ArtifactRules.IsArtifact(context.FileSystem, dir),
                context.Guard.CanDescend)
            .Select(dir => new CleanupPath(dir, Description: DirectorySweep.LeafName(dir)));

    protected override IEnumerable<string> ScanNotes(CleanupContext context) => WorkspaceRoots.Refusals(context);
}

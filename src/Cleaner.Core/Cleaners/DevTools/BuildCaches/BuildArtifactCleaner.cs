using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Utils;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// Build outputs, dependency folders, virtualenvs, and tool caches of projects under the scan roots
/// (<c>--path</c>, default current directory).
/// </summary>
/// <remarks>
/// A folder is taken only when <see cref="BuildOutput"/> proves it is generated output (e.g. a <c>bin</c>
/// needs a .NET project and a restore beside it). The scan refuses the profile, drive roots, and system
/// folders, never enters links, dot-folders, or installed tools, and is opt-in so "All cleaners" skips it.
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
        DirectorySearch.Find(
                context.FileSystem,
                ProjectRoots.Allowed(context),
                dir => BuildOutput.Matches(context.FileSystem, dir),
                context.Guard.CanEnter)
            .Select(dir => new CleanupPath(dir, Description: PathComparison.Name(dir)));

    protected override IEnumerable<string> ScanNotes(CleanupContext context) => ProjectRoots.Refusals(context);
}

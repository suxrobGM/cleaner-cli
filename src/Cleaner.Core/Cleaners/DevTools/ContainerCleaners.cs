using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// Prunes Docker disk: <c>system prune -f</c> + <c>builder prune -af</c> by default; with
/// <c>--force</c>, <c>system prune -a --volumes -f</c> also removes unused images and named volumes
/// (can delete data), so it stays behind the cautious-targets flag. On Docker Desktop/WSL2 this only
/// frees space inside the <c>.vhdx</c>; the host file shrinks after a separate compaction.
/// </summary>
public sealed class DockerCleaner : ProcessCleanerBase
{
    public override string Id => "docker";

    public override string Name => "Docker (system prune)";

    public override string Category => Categories.Containers;

    public override bool SupportsSizeEstimate => false;

    protected override string Executable => "docker";

    // The declared safe command, used for the dry-run/missing-tool fallback path.
    protected override IReadOnlyList<string> CleanArguments => ["system", "prune", "--force"];

    protected override IEnumerable<IReadOnlyList<string>> CommandSequence(CleanupContext context)
    {
        yield return context.Force
            ? ["system", "prune", "-a", "--volumes", "--force"]
            : ["system", "prune", "--force"];
        yield return ["builder", "prune", "--all", "--force"];
    }

    // Docker's storage lives in the daemon's data root, which isn't a user-accessible directory we
    // can size or delete — the prune commands are the only safe interface.
    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) => [];
}

/// <summary>Terraform provider plugin cache.</summary>
public sealed class TerraformCleaner : DirectoryCleanerBase
{
    public override string Id => "terraform";

    public override string Name => "Terraform plugin cache";

    public override string Category => Categories.Containers;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        [new CleanupPath(context.Environment.HomePath(".terraform.d", "plugin-cache"), DeleteMode.ClearContents)];
}

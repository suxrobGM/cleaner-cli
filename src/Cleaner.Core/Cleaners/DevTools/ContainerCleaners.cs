using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// Reclaims Docker disk via <c>docker system prune -f</c> (dangling images, stopped containers,
/// unused networks, and build cache). Size is reported by Docker, not pre-measured from disk.
/// </summary>
public sealed class DockerCleaner : ProcessCleanerBase
{
    public override string Id => "docker";

    public override string Name => "Docker (system prune)";

    public override string Category => Categories.Containers;

    protected override string Executable => "docker";

    protected override IReadOnlyList<string> CleanArguments => ["system", "prune", "--force"];

    // Docker's storage lives in the daemon's data root, which isn't a user-accessible directory we
    // can size or delete — the prune command is the only safe interface.
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

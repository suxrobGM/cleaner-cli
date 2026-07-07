using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>Prunes Podman storage; like Docker, the prune commands are the only safe interface.</summary>
public sealed class PodmanCleaner : ProcessCleanerBase
{
    public override string Id => "podman";

    public override string Name => "Podman (system prune)";

    public override string Category => Categories.Containers;

    public override bool SupportsSizeEstimate => false;

    protected override string Executable => "podman";

    protected override IReadOnlyList<string> CleanArguments => ["system", "prune", "--force"];

    protected override IEnumerable<IReadOnlyList<string>> CommandSequence(CleanupContext context)
    {
        yield return context.Force
            ? ["system", "prune", "-a", "--volumes", "--force"]
            : ["system", "prune", "--force"];
    }

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) => [];
}

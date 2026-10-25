using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// Prunes Podman storage through its prune commands, the only safe interface. Named volumes hold user
/// data and are kept.
/// </summary>
public sealed class PodmanCleaner : ProcessCleanerBase
{
    public override string Id => "podman";

    public override string Name => "Podman (system prune)";

    public override string Category => Categories.Containers;

    public override bool SupportsSizeEstimate => false;

    protected override string Executable => "podman";

    protected override IReadOnlyList<string> CleanArguments => ["system", "prune", "-a", "--force"];
}

using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.Os;

/// <summary>Vacuums the systemd journal to a small size (Linux). Needs elevation.</summary>
public sealed class JournalLogCleaner : ProcessCleanerBase
{
    public override string Id => "journal";

    public override string Name => "systemd journal logs";

    public override string Category => Categories.OperatingSystem;

    public override bool RequiresElevation => true;

    public override bool IsApplicable(CleanupContext context) => context.Environment.IsLinux;

    protected override string Executable => "journalctl";

    protected override IReadOnlyList<string> CleanArguments => ["--vacuum-size=100M"];

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) => [];
}

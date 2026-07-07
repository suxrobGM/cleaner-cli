using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Services;

namespace Cleaner.Core.Cleaners.Os;

/// <summary>
/// Windows component store (WinSxS) cleanup via <c>DISM /StartComponentCleanup</c> — removes
/// superseded component versions. Deliberately no <c>/ResetBase</c>, which would prevent
/// uninstalling updates. Slow (minutes) but the largest legitimate Windows reclaim.
/// </summary>
public sealed class WinSxSCleaner : ProcessCleanerBase
{
    public override string Id => "winsxs";

    public override string Name => "Windows component store (WinSxS)";

    public override string Category => Categories.OperatingSystem;

    public override bool RequiresElevation => true;

    public override bool SupportsSizeEstimate => false;

    public override bool IsApplicable(CleanupContext context) => context.Environment.IsWindows;

    protected override string Executable => "dism";

    protected override IReadOnlyList<string> CleanArguments =>
        ["/Online", "/Cleanup-Image", "/StartComponentCleanup"];

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) => [];
}

using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Services;

namespace Cleaner.Core.Cleaners.Os;

/// <summary>
/// Configurable cleaner for system package managers (apt, dnf, pacman, brew, scoop, and choco).
/// Instances provide their command and applicability from the composition root.
/// </summary>
/// <param name="followUpArguments">Further commands run after <paramref name="cleanArguments"/>, in order.</param>
/// <param name="confirmationWarning">Shown before the run when the cleanup has a trade-off.</param>
public sealed class SystemPackageManagerCleaner(
    string id,
    string name,
    string executable,
    IReadOnlyList<string> cleanArguments,
    bool requiresElevation,
    Func<IEnvironmentService, bool> isApplicable,
    Func<CleanupContext, IEnumerable<CleanupPath>>? targets = null,
    IReadOnlyList<IReadOnlyList<string>>? followUpArguments = null,
    string? confirmationWarning = null) : ProcessCleanerBase
{
    public override string Id => id;

    public override string Name => name;

    public override string Category => Categories.SystemPackageManagers;

    public override bool RequiresElevation => requiresElevation;

    // Without declared directories the size is only knowable after the command runs.
    public override bool SupportsSizeEstimate => targets is not null;

    public override string? ConfirmationWarning => confirmationWarning;

    public override bool IsApplicable(CleanupContext context) => isApplicable(context.Environment);

    protected override string Executable => executable;

    protected override IReadOnlyList<string> CleanArguments => cleanArguments;

    protected override IEnumerable<IReadOnlyList<string>> CommandSequence(CleanupContext context) =>
        [cleanArguments, .. followUpArguments ?? []];

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        targets?.Invoke(context) ?? [];
}

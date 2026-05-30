using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Services;

namespace Cleaner.Core.Cleaners.Os;

/// <summary>
/// A configurable cleaner for system package managers (apt, dnf, pacman, brew, scoop, choco).
/// Each instance is constructed in the composition root with its command and applicability, so a
/// new manager is one registration line rather than a new class.
/// </summary>
public sealed class SystemPackageManagerCleaner(
    string id,
    string name,
    string executable,
    IReadOnlyList<string> cleanArguments,
    bool requiresElevation,
    Func<IEnvironmentService, bool> isApplicable,
    Func<CleanupContext, IEnumerable<CleanupPath>>? targets = null) : ProcessCleanerBase
{
    public override string Id => id;

    public override string Name => name;

    public override string Category => Categories.SystemPackageManagers;

    public override bool RequiresElevation => requiresElevation;

    public override bool IsApplicable(CleanupContext context) => isApplicable(context.Environment);

    protected override string Executable => executable;

    protected override IReadOnlyList<string> CleanArguments => cleanArguments;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        targets?.Invoke(context) ?? [];
}

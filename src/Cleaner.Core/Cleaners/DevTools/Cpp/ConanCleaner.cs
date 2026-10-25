using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// Conan (C/C++) cache via <c>conan cache clean "*"</c>: source, build, download, and temporary
/// folders, rebuilt on the next install.
/// </summary>
/// <remarks>
/// Package binaries are kept: <c>conan remove</c> would also drop packages built locally or exported
/// with <c>conan create</c>/<c>conan export</c>, which no remote can restore. Nothing is deleted
/// directly, so the cleaner is offered only when <c>conan</c> is on PATH.
/// </remarks>
public sealed class ConanCleaner : ProcessCleanerBase
{
    public override string Id => "conan";

    public override string Name => "Conan cache";

    public override string Category => Categories.Languages;

    // Without declared directories the size is only knowable after the command runs.
    public override bool SupportsSizeEstimate => false;

    protected override string Executable => "conan";

    protected override IReadOnlyList<string> CleanArguments => ["cache", "clean", "*"];
}

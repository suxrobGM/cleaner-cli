using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// Conan (C/C++) source, build, download, and temp folders via <c>conan cache clean "*"</c>; package
/// binaries are kept.
/// </summary>
/// <remarks>
/// <c>conan remove</c> is avoided because it also drops locally built or exported packages that no remote
/// can restore. Nothing is deleted directly, so the cleaner needs <c>conan</c> on PATH.
/// </remarks>
public sealed class ConanCleaner : ProcessCleanerBase
{
    public override string Id => "conan";

    public override string Name => "Conan cache";

    public override string Category => Categories.Languages;

    // With no known directories, the size is only known after the command runs.
    public override bool SupportsSizeEstimate => false;

    protected override string Executable => "conan";

    protected override IReadOnlyList<string> CleanArguments => ["cache", "clean", "*"];
}

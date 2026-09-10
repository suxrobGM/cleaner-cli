using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.Os;

/// <summary>Base for Windows-only cleaners.</summary>
public abstract class WindowsCleanerBase : DirectoryCleanerBase
{
    public override string Category => Categories.OperatingSystem;

    public override bool IsApplicable(CleanupContext context) => context.Environment.IsWindows;

    /// <summary>
    /// A path under <c>%ProgramData%</c>, or null when neither the variable nor the Windows
    /// directory resolves. Falls back to the Windows drive root so a relocated install still works.
    /// </summary>
    protected static string? ProgramDataPath(CleanupContext context, params string[] segments)
    {
        var env = context.Environment;
        var programData = OsPaths.Env(env, "ProgramData")
            ?? (env.WindowsDirectory is { } windows ? OsPaths.FromWindowsDriveRoot(windows, "ProgramData") : null);

        return programData is null ? null : Path.Combine([programData, .. segments]);
    }
}

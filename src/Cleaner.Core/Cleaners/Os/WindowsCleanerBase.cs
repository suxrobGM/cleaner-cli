using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Services;

namespace Cleaner.Core.Cleaners.Os;

/// <summary>Base for Windows-only cleaners.</summary>
public abstract class WindowsCleanerBase : DirectoryCleanerBase
{
    public override string Category => Categories.OperatingSystem;

    public override bool IsApplicable(CleanupContext context) => context.Environment.IsWindows;
}

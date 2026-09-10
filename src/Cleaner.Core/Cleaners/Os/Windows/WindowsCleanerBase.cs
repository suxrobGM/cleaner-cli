using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.Os;

/// <summary>Base for Windows-only cleaners.</summary>
public abstract class WindowsCleanerBase : DirectoryCleanerBase
{
    public override string Category => Categories.SystemCaches;

    public override bool IsApplicable(CleanupContext context) => context.Environment.IsWindows;
}

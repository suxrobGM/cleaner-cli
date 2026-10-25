using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Utils;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>cpanm (Perl) build work directories (~/.cpanm/work-*).</summary>
public sealed class CpanmCleaner : DirectoryCleanerBase
{
    public override string Id => "cpanm";

    public override string Name => "cpanm work directories";

    public override string Category => Categories.Languages;

    public override bool IsApplicable(CleanupContext context) => !context.Environment.IsWindows;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var root = context.Environment.HomePath(".cpanm");
        foreach (var dir in context.FileSystem.EnumerateDirectories(root))
        {
            if (PathComparison.Name(dir).StartsWith("work", StringComparison.OrdinalIgnoreCase))
            {
                yield return new CleanupPath(dir, Description: "build workspace");
            }
        }
    }
}

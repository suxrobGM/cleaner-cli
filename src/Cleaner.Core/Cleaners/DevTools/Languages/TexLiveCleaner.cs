using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>TeX Live font/luatex caches under ~/.texlive*/texmf-var (regenerated on use).</summary>
public sealed class TexLiveCleaner : DirectoryCleanerBase
{
    public override string Id => "texlive";

    public override string Name => "TeX Live font caches";

    public override string Category => Categories.Languages;

    public override bool IsApplicable(CleanupContext context) => !context.Environment.IsWindows;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        foreach (var dir in context.FileSystem.EnumerateDirectories(context.Environment.HomeDirectory))
        {
            if (!DirectorySweep.LeafName(dir).StartsWith(".texlive", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            yield return new CleanupPath(Path.Combine(dir, "texmf-var", "luatex-cache"), Description: "luatex cache");
            yield return new CleanupPath(Path.Combine(dir, "texmf-var", "fonts"), Description: "font cache");
        }
    }
}

using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>Visual Studio caches: project-local <c>.vs</c> and the ComponentModelCache (Windows).</summary>
public sealed class VisualStudioCleaner : DirectoryCleanerBase
{
    public override string Id => "visualstudio";

    public override string Name => "Visual Studio caches";

    public override string Category => Categories.Ides;

    public override bool IsApplicable(CleanupContext context) => context.Environment.IsWindows;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return new CleanupPath(Path.Combine(context.WorkingDirectory, ".vs"), Description: "project .vs");

        var vsRoot = Path.Combine(env.LocalAppDataDirectory, "Microsoft", "VisualStudio");
        foreach (var versionDir in context.FileSystem.EnumerateDirectories(vsRoot))
        {
            yield return new CleanupPath(Path.Combine(versionDir, "ComponentModelCache"), Description: "ComponentModelCache");
        }
    }
}

using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// VS Code cache directories (keeps settings and installed extensions). Also covers forks with the
/// same layout: Cursor, VSCodium, and Windsurf.
/// </summary>
public sealed class VsCodeCleaner : DirectoryCleanerBase
{
    private static readonly string[] CacheSubdirectories =
        ["Cache", "CachedData", "Code Cache", "GPUCache", "logs", "CachedExtensionVSIXs"];

    /// <summary>App-data folder names of VS Code and its forks.</summary>
    private static readonly string[] AppFolders = ["Code", "Cursor", "VSCodium", "Windsurf"];

    public override string Id => "vscode";

    public override string Name => "VS Code / Cursor / VSCodium caches";

    public override string Category => Categories.Ides;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        foreach (var app in AppFolders)
        {
            var userRoot = env.IsWindows
                ? Path.Combine(env.AppDataDirectory, app)
                : env.IsMacOs
                    ? Path.Combine(env.HomeDirectory, "Library", "Application Support", app)
                    : Path.Combine(env.HomeDirectory, ".config", app);

            foreach (var sub in CacheSubdirectories)
            {
                yield return new CleanupPath(Path.Combine(userRoot, sub), Description: $"{app} {sub}");
            }
        }
    }
}

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
    [
        "Cache", "CachedData", "Code Cache", "GPUCache", "logs", "CachedExtensionVSIXs",
        // Webview localStorage and crash reports: both regenerate, and WebStorage is usually the
        // largest of them because nothing prunes it as extensions come and go.
        "WebStorage", "Crashpad",
    ];

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
            var userRoot = OsPaths.AppData(env, app, app, app);

            foreach (var sub in CacheSubdirectories)
            {
                yield return new CleanupPath(Path.Combine(userRoot, sub), Description: $"{app} {sub}");
            }
        }
    }
}

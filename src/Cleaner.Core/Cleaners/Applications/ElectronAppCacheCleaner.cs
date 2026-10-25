using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Services;

namespace Cleaner.Core.Cleaners.Applications;

/// <summary>
/// Chromium HTTP, GPU, and shader caches of known Electron apps. Configuration, logs, and local
/// storage are kept.
/// </summary>
public sealed class ElectronAppCacheCleaner : DirectoryCleanerBase
{
    /// <summary>App folders relative to the per-OS app-data root.</summary>
    private static readonly (string Label, string[] Segments)[] Apps =
    [
        ("Discord", ["discord"]),
        ("Slack", ["Slack"]),
        ("Microsoft Teams", ["Microsoft", "Teams"]),
        ("Claude", ["Claude"]),
        ("MongoDB Compass", ["MongoDB Compass"]),
        ("Postman", ["Postman"]),
        ("Notion", ["Notion"]),
        ("Obsidian", ["obsidian"]),
        ("Figma", ["Figma"]),
        ("Signal", ["Signal"]),
        ("GitHub Desktop", ["GitHub Desktop"]),
        ("WhatsApp", ["WhatsApp"]),
        ("Element", ["Element"]),
    ];

    public override string Id => "electron-app-cache";

    public override string Name => "Electron app caches (Discord/Slack/Teams/Claude/…)";

    public override string Category => Categories.DesktopApps;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        var root = OsPaths.AppDataRoot(env);
        foreach (var (label, segments) in Apps)
        {
            var appRoot = Path.Combine([root, .. segments]);
            foreach (var path in ChromiumCache.Under(appRoot, label))
            {
                yield return path;
            }
        }

        // New Teams (the Store/WebView2 app) keeps its caches in its package's EBWebView profile,
        // not under %APPDATA%; the Teams entry above covers the classic Electron client.
        if (env.IsWindows)
        {
            var webView = Path.Combine(
                env.LocalAppDataDirectory, "Packages", "MSTeams_8wekyb3d8bbwe",
                "LocalCache", "Microsoft", "MSTeams", "EBWebView", "Default");
            foreach (var path in ChromiumCache.Under(webView, "Microsoft Teams (new)"))
            {
                yield return path;
            }
        }
    }
}

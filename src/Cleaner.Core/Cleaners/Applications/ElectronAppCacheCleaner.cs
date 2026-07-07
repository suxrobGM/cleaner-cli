using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Services;

namespace Cleaner.Core.Cleaners.Applications;

/// <summary>
/// Clears the Chromium HTTP/GPU/shader caches of common Electron desktop apps (Discord, Slack,
/// Microsoft Teams, Claude, and more). Each app stores its data under a per-OS root; we only touch
/// the cache subdirectories (see <see cref="ChromiumCache"/>) and never config, logs, or local storage.
/// </summary>
public sealed class ElectronAppCacheCleaner : DirectoryCleanerBase
{
    /// <summary>Folder name segments of each known app, relative to the per-OS app-data root.</summary>
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

    public override string Category => Categories.Applications;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        var root = AppDataRoot(env);
        foreach (var (label, segments) in Apps)
        {
            var appRoot = Path.Combine([root, .. segments]);
            foreach (var path in ChromiumCache.Under(appRoot, label))
            {
                yield return path;
            }
        }

        // New Microsoft Teams (the Store/WebView2 app) keeps its Chromium caches inside its
        // package's EBWebView profile rather than under %APPDATA%; the classic Teams entry in
        // the table above covers the old Electron client.
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

    /// <summary>Where Electron apps keep per-user data: %APPDATA% on Windows, the usual roots elsewhere.</summary>
    private static string AppDataRoot(IEnvironmentService env)
    {
        if (env.IsWindows)
        {
            return env.AppDataDirectory;
        }

        return env.IsMacOs
            ? Path.Combine(env.HomeDirectory, "Library", "Application Support")
            : Path.Combine(env.HomeDirectory, ".config");
    }
}

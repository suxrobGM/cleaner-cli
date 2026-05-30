using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Services;

namespace Cleaner.Core.Cleaners.Applications;

/// <summary>
/// Clears the Chromium HTTP/GPU/shader caches of common Electron desktop apps (Discord, Slack,
/// Microsoft Teams). Each app stores its data under a per-OS root; we only touch the cache
/// subdirectories (see <see cref="ChromiumCache"/>) and never config, logs, or local storage.
/// </summary>
public sealed class ElectronAppCacheCleaner : DirectoryCleanerBase
{
    /// <summary>Folder name segments of each known app, relative to the per-OS app-data root.</summary>
    private static readonly (string Label, string[] Segments)[] Apps =
    [
        ("Discord", ["discord"]),
        ("Slack", ["Slack"]),
        ("Microsoft Teams", ["Microsoft", "Teams"]),
    ];

    public override string Id => "electron-app-cache";

    public override string Name => "Electron app caches (Discord/Slack/Teams)";

    public override string Category => Categories.Applications;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var root = AppDataRoot(context.Environment);
        foreach (var (label, segments) in Apps)
        {
            var appRoot = Path.Combine([root, .. segments]);
            foreach (var path in ChromiumCache.Under(appRoot, label))
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

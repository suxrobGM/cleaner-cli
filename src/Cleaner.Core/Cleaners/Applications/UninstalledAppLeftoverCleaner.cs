using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Services;

namespace Cleaner.Core.Cleaners.Applications;

/// <summary>
/// Per-user profile directories left by uninstalled applications.
/// </summary>
/// <remarks>
/// Targets are curated and offered only when every sign of an install is absent: the install
/// folders, an MSIX package, a PATH entry into the app's folders and, on Windows, an uninstall
/// record in the registry. A failed or empty registry read is treated as unknown and offers nothing;
/// broad AppData name-matching could remove data for live tools.
/// </remarks>
public sealed class UninstalledAppLeftoverCleaner : DirectoryCleanerBase
{
    /// <summary>The uninstall keys an installed app registers its <c>DisplayName</c> under.</summary>
    private static readonly string[] UninstallKeys =
    [
        @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
        @"HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall",
        @"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
    ];

    private static readonly TimeSpan RegistryTimeout = TimeSpan.FromSeconds(30);

    /// <summary>Leftovers found for the current run context, so the clean does not query again.</summary>
    private (CleanupContext Context, IReadOnlyList<CleanupPath> Targets)? _leftovers;

    /// <summary>
    /// One app: how to tell it is installed, and what it leaves behind if it isn't.
    /// </summary>
    /// <param name="InstallMarkers">Folders that exist only while the app is installed.</param>
    /// <param name="RegistryNames">Text any uninstall <c>DisplayName</c> of the app contains.</param>
    /// <param name="MsixPrefixes">Name prefixes of its MSIX package folders under <c>%LOCALAPPDATA%\Packages</c>.</param>
    private readonly record struct AppLeftovers(
        string Name,
        string[] InstallMarkers,
        string[] DataDirectories,
        string[] RegistryNames,
        string[] MsixPrefixes);

    public override string Id => "app-leftovers";

    public override string Name => "Uninstalled app leftovers";

    public override string Category => Categories.DesktopApps;

    public override string ConfirmationWarning =>
        "these are settings, logs and history from apps that appear to be uninstalled, not caches — " +
        "reinstalling one of them will start from a clean profile";

    public override bool IsApplicable(CleanupContext context) =>
        context.Environment.IsWindows || context.Environment.IsMacOs;

    /// <summary>
    /// Relevant wherever an app looks uninstalled on disk and left data behind; whether the
    /// registry agrees costs a scan, so it is asked only there.
    /// </summary>
    public override bool IsAvailable(CleanupContext context) =>
        LooksUninstalled(context).Any(app => app.DataDirectories.Any(context.FileSystem.DirectoryExists));

    protected override async ValueTask<IEnumerable<CleanupPath>> GetTargetsAsync(
        CleanupContext context,
        CancellationToken cancellationToken)
    {
        if (_leftovers is { } cached && ReferenceEquals(cached.Context, context))
        {
            return cached.Targets;
        }

        var targets = await LeftoversAsync(context, cancellationToken).ConfigureAwait(false);
        _leftovers = (context, targets);
        return targets;
    }

    private static async Task<IReadOnlyList<CleanupPath>> LeftoversAsync(
        CleanupContext context,
        CancellationToken cancellationToken)
    {
        var candidates = LooksUninstalled(context)
            .Where(app => app.DataDirectories.Any(context.FileSystem.DirectoryExists))
            .ToList();

        if (candidates.Count == 0)
        {
            return [];
        }

        if (context.Environment.IsWindows)
        {
            // No answer from the registry means "unknown", never "nothing is installed".
            var installed = await InstalledDisplayNamesAsync(context, cancellationToken).ConfigureAwait(false);
            if (installed.Count == 0)
            {
                return [];
            }

            candidates.RemoveAll(app => app.RegistryNames.Any(name =>
                installed.Any(displayName => displayName.Contains(name, StringComparison.OrdinalIgnoreCase))));
        }

        return [.. candidates.SelectMany(app =>
            app.DataDirectories.Select(directory => new CleanupPath(directory, Description: app.Name)))];
    }

    /// <summary>Apps with no install folder, MSIX package, or PATH entry left on this machine.</summary>
    private static IEnumerable<AppLeftovers> LooksUninstalled(CleanupContext context)
    {
        var path = PathEntries(context.Environment);
        foreach (var app in KnownApps(context.Environment))
        {
            // Any surviving marker means the app is still installed; leave its data alone.
            if (app.InstallMarkers.Any(context.FileSystem.DirectoryExists)
                || HasMsixPackage(context, app)
                || path.Any(entry => app.InstallMarkers.Concat(app.DataDirectories).Any(dir => IsWithin(entry, dir))))
            {
                continue;
            }

            yield return app;
        }
    }

    private static bool HasMsixPackage(CleanupContext context, AppLeftovers app)
    {
        if (app.MsixPrefixes.Length == 0)
        {
            return false;
        }

        var packages = Path.Combine(context.Environment.LocalAppDataDirectory, "Packages");
        return context.FileSystem.DirectoryExists(packages)
            && context.FileSystem.EnumerateDirectories(packages)
                .Select(Path.GetFileName)
                .Any(name => app.MsixPrefixes.Any(prefix =>
                    name?.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) == true));
    }

    private static string[] PathEntries(IEnvironmentService env) =>
        (env.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(env.IsWindows ? ';' : ':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(entry => entry.Trim('"'))
            .Where(entry => entry.Length > 0)
            .ToArray();

    /// <summary>
    /// Whether <paramref name="path"/> is <paramref name="directory"/> or inside it. Compared with
    /// both separators unified and case ignored, as on the Windows and macOS volumes this runs on.
    /// </summary>
    private static bool IsWithin(string path, string directory)
    {
        var child = Unify(path);
        var parent = Unify(directory);
        return child.Equals(parent, StringComparison.OrdinalIgnoreCase)
            || child.StartsWith(parent + '/', StringComparison.OrdinalIgnoreCase);

        static string Unify(string p) => p.Replace('\\', '/').TrimEnd('/');
    }

    /// <summary>
    /// Every <c>DisplayName</c> under the uninstall keys. Keys that fail to read (such as
    /// WOW6432Node on a 32-bit system) are skipped; an empty set means nothing could be read.
    /// </summary>
    private static async Task<List<string>> InstalledDisplayNamesAsync(
        CleanupContext context,
        CancellationToken cancellationToken)
    {
        var names = new List<string>();
        foreach (var key in UninstallKeys)
        {
            var result = await context.ProcessRunner
                .RunAsync("reg", ["query", key, "/s", "/v", "DisplayName"], RegistryTimeout, cancellationToken)
                .ConfigureAwait(false);

            if (!result.Success)
            {
                continue;
            }

            foreach (var line in result.StandardOutput.Split('\n'))
            {
                // "    DisplayName    REG_SZ    Slack"
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("DisplayName", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var type = trimmed.IndexOf("REG_", StringComparison.OrdinalIgnoreCase);
                var value = type < 0 ? -1 : trimmed.IndexOfAny([' ', '\t'], type);
                if (value >= 0 && trimmed[value..].Trim() is { Length: > 0 } name)
                {
                    names.Add(name);
                }
            }
        }

        return names;
    }

    private static IEnumerable<AppLeftovers> KnownApps(IEnvironmentService env) =>
        env.IsWindows ? WindowsApps(env) : MacApps(env);

    private static IEnumerable<AppLeftovers> WindowsApps(IEnvironmentService env)
    {
        var local = env.LocalAppDataDirectory;
        var roaming = env.AppDataDirectory;
        // Without the install roots there is no way to tell an uninstalled app from an installed
        // one, and the markers would silently pass — offer nothing rather than guess.
        if (OsPaths.ProgramFiles(env) is not { } programFiles
            || OsPaths.ProgramFiles(env, x86: true) is not { } programFilesX86)
        {
            yield break;
        }


        // Claude Desktop. Deliberately excludes ~/.claude, %LOCALAPPDATA%\claude-cli-nodejs and
        // %LOCALAPPDATA%\ClaudeCodeExtension: those belong to Claude Code (the CLI and the editor
        // extension), a separate product that is often installed while the desktop app is not.
        yield return new AppLeftovers(
            "Claude Desktop",
            [Path.Combine(local, "AnthropicClaude"), Path.Combine(local, "Programs", "Claude")],
            [Path.Combine(roaming, "Claude"), Path.Combine(roaming, "Claude-3p"), Path.Combine(local, "Claude")],
            ["Claude"],
            ["Claude_", "AnthropicPBC"]);

        // Docker Desktop keeps its WSL2 virtual disk under %LOCALAPPDATA%\Docker; with the app gone
        // that file is dead weight and is usually the largest leftover on a dev machine.
        yield return new AppLeftovers(
            "Docker Desktop",
            [Path.Combine(programFiles, "Docker"), Path.Combine(local, "Docker Desktop")],
            [Path.Combine(local, "Docker"), Path.Combine(roaming, "Docker"), Path.Combine(roaming, "Docker Desktop")],
            ["Docker Desktop"],
            []);

        yield return new AppLeftovers(
            "Discord",
            [Path.Combine(local, "Discord")],
            [Path.Combine(roaming, "discord")],
            ["Discord"],
            []);

        yield return new AppLeftovers(
            "Slack",
            [Path.Combine(local, "slack")],
            [Path.Combine(roaming, "Slack")],
            ["Slack"],
            ["91750D7E.Slack_"]);

        yield return new AppLeftovers(
            "Unity Hub",
            [Path.Combine(programFiles, "Unity Hub"), Path.Combine(programFilesX86, "Unity Hub")],
            [Path.Combine(roaming, "UnityHub"), Path.Combine(local, "unityhub-updater")],
            ["Unity Hub"],
            []);

        yield return new AppLeftovers(
            "Epic Games Launcher",
            [Path.Combine(programFiles, "Epic Games"), Path.Combine(programFilesX86, "Epic Games")],
            [Path.Combine(local, "EpicGamesLauncher")],
            ["Epic Games Launcher"],
            []);
    }

    private static IEnumerable<AppLeftovers> MacApps(IEnvironmentService env)
    {
        var support = Path.Combine(env.HomeDirectory, "Library", "Application Support");
        var caches = Path.Combine(env.HomeDirectory, "Library", "Caches");
        var logs = Path.Combine(env.HomeDirectory, "Library", "Logs");

        // Apps can be installed system-wide or per user (~/Applications); either one counts.
        string[] Bundle(string app) => [Path.Combine("/Applications", app), Path.Combine(env.HomeDirectory, "Applications", app)];

        yield return new AppLeftovers(
            "Claude Desktop",
            Bundle("Claude.app"),
            [Path.Combine(support, "Claude"), Path.Combine(caches, "Claude"), Path.Combine(logs, "Claude")],
            [],
            []);

        yield return new AppLeftovers(
            "Docker Desktop",
            Bundle("Docker.app"),
            [Path.Combine(support, "Docker Desktop"), Path.Combine(caches, "com.docker.docker")],
            [],
            []);

        yield return new AppLeftovers(
            "Discord",
            Bundle("Discord.app"),
            [Path.Combine(support, "discord")],
            [],
            []);

        yield return new AppLeftovers(
            "Slack",
            Bundle("Slack.app"),
            [Path.Combine(support, "Slack")],
            [],
            []);
    }
}

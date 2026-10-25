using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Services;
using Cleaner.Core.Utils;

namespace Cleaner.Core.Cleaners.Applications;

/// <summary>Profile folders left behind by a fixed list of uninstalled apps.</summary>
/// <remarks>
/// An app's folders are offered only when every sign of an install is gone: install folders, MSIX
/// package, PATH entry and, on Windows, an uninstall registry entry. A failed registry read offers
/// nothing. The list is fixed because matching AppData by name could delete data of installed tools.
/// </remarks>
public sealed class UninstalledAppLeftoverCleaner : DirectoryCleanerBase
{
    private static readonly string[] UninstallKeys =
    [
        @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
        @"HKLM\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall",
        @"HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
    ];

    private static readonly TimeSpan RegistryTimeout = TimeSpan.FromSeconds(30);

    /// <summary>How to tell an app is installed, and what it leaves behind once it is not.</summary>
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

    /// <summary>Checks the disk only; the slower registry check is left to the scan.</summary>
    public override bool IsAvailable(CleanupContext context) =>
        LooksUninstalled(context).Any(app => app.DataDirectories.Any(context.FileSystem.DirectoryExists));

    protected override async ValueTask<IEnumerable<CleanupPath>> GetTargetsAsync(
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
        var env = context.Environment;
        var packages = new Lazy<string[]>(() => MsixPackageNames(context));
        foreach (var app in KnownApps(env))
        {
            // Any surviving marker means the app is still installed; leave its data alone.
            if (app.InstallMarkers.Any(context.FileSystem.DirectoryExists)
                || app.MsixPrefixes.Any(prefix =>
                    packages.Value.Any(name => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                || env.PathDirectories.Any(entry => app.InstallMarkers.Concat(app.DataDirectories)
                    .Any(dir => PathComparison.IsSameOrUnder(entry, dir, env.IsLinux))))
            {
                continue;
            }

            yield return app;
        }
    }

    /// <summary>Folder names under <c>%LOCALAPPDATA%\Packages</c>, listed once per check.</summary>
    private static string[] MsixPackageNames(CleanupContext context)
    {
        var packages = Path.Combine(context.Environment.LocalAppDataDirectory, "Packages");
        return context.FileSystem.DirectoryExists(packages)
            ? [.. context.FileSystem.EnumerateDirectories(packages).Select(PathComparison.Name)]
            : [];
    }

    /// <summary>
    /// Every <c>DisplayName</c> under the uninstall keys. Keys that fail to read (such as
    /// WOW6432Node on a 32-bit system) are skipped; an empty set means nothing could be read.
    /// </summary>
    private static async Task<List<string>> InstalledDisplayNamesAsync(
        CleanupContext context,
        CancellationToken cancellationToken)
    {
        // The keys are independent, so query them together.
        var results = await Task.WhenAll(UninstallKeys.Select(key => context.ProcessRunner
                .RunAsync("reg", ["query", key, "/s", "/v", "DisplayName"], RegistryTimeout, cancellationToken)))
            .ConfigureAwait(false);

        var names = new List<string>();
        foreach (var result in results)
        {
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
        // Without the Program Files paths the install markers can't be checked, so offer nothing.
        if (OsPaths.ProgramFiles(env) is not { } programFiles
            || OsPaths.ProgramFiles(env, x86: true) is not { } programFilesX86)
        {
            yield break;
        }

        // Leaves out ~/.claude, %LOCALAPPDATA%\claude-cli-nodejs and %LOCALAPPDATA%\ClaudeCodeExtension:
        // they belong to Claude Code, which is often installed without the desktop app.
        yield return new AppLeftovers(
            "Claude Desktop",
            [Path.Combine(local, "AnthropicClaude"), Path.Combine(local, "Programs", "Claude")],
            [Path.Combine(roaming, "Claude"), Path.Combine(roaming, "Claude-3p"), Path.Combine(local, "Claude")],
            ["Claude"],
            ["Claude_", "AnthropicPBC"]);

        // %LOCALAPPDATA%\Docker holds the WSL2 disk image, usually the largest leftover on a dev machine.
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

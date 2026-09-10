using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Services;

namespace Cleaner.Core.Cleaners.Applications;

/// <summary>
/// Profile directories left behind by uninstalled applications. Uninstallers routinely drop the
/// program but keep its per-user data, which runs to gigabytes for an Electron app bundling a
/// runtime or VM image.
/// </summary>
/// <remarks>
/// Each app pairs its install markers with the directories it leaves behind, and leftovers are
/// offered only when every marker is gone. The list is curated rather than inferred because
/// name-matching over <c>AppData</c> flags live tools like nvm and vcpkg.
/// </remarks>
public sealed class UninstalledAppLeftoverCleaner : DirectoryCleanerBase
{
    /// <summary>One app: how to tell it is installed, and what it leaves behind if it isn't.</summary>
    private readonly record struct AppLeftovers(string Name, string[] InstallMarkers, string[] DataDirectories);

    public override string Id => "app-leftovers";

    public override string Name => "Uninstalled app leftovers";

    public override string Category => Categories.Applications;

    public override string ConfirmationWarning =>
        "these are settings, logs and history from apps that appear to be uninstalled, not caches — " +
        "reinstalling one of them will start from a clean profile";

    public override bool IsApplicable(CleanupContext context) =>
        context.Environment.IsWindows || context.Environment.IsMacOs;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        foreach (var app in KnownApps(context.Environment))
        {
            // Any surviving marker means the app is still installed; leave its data alone.
            if (app.InstallMarkers.Any(context.FileSystem.DirectoryExists))
            {
                continue;
            }

            foreach (var directory in app.DataDirectories)
            {
                yield return new CleanupPath(directory, Description: app.Name);
            }
        }
    }

    private static IEnumerable<AppLeftovers> KnownApps(IEnvironmentService env) =>
        env.IsWindows ? WindowsApps(env) : MacApps(env);

    private static IEnumerable<AppLeftovers> WindowsApps(IEnvironmentService env)
    {
        var local = env.LocalAppDataDirectory;
        var roaming = env.AppDataDirectory;
        var programFiles = OsPaths.Env(env, "ProgramFiles") ?? @"C:\Program Files";
        var programFilesX86 = OsPaths.Env(env, "ProgramFiles(x86)") ?? @"C:\Program Files (x86)";

        // Claude Desktop. Deliberately excludes ~/.claude, %LOCALAPPDATA%\claude-cli-nodejs and
        // %LOCALAPPDATA%\ClaudeCodeExtension: those belong to Claude Code (the CLI and the editor
        // extension), a separate product that is often installed while the desktop app is not.
        yield return new AppLeftovers(
            "Claude Desktop",
            [Path.Combine(local, "AnthropicClaude"), Path.Combine(local, "Programs", "Claude")],
            [Path.Combine(roaming, "Claude"), Path.Combine(roaming, "Claude-3p"), Path.Combine(local, "Claude")]);

        // Docker Desktop keeps its WSL2 virtual disk under %LOCALAPPDATA%\Docker; with the app gone
        // that file is dead weight and is usually the largest leftover on a dev machine.
        yield return new AppLeftovers(
            "Docker Desktop",
            [Path.Combine(programFiles, "Docker"), Path.Combine(local, "Docker Desktop")],
            [Path.Combine(local, "Docker"), Path.Combine(roaming, "Docker"), Path.Combine(roaming, "Docker Desktop")]);

        yield return new AppLeftovers(
            "Discord",
            [Path.Combine(local, "Discord")],
            [Path.Combine(roaming, "discord")]);

        yield return new AppLeftovers(
            "Slack",
            [Path.Combine(local, "slack")],
            [Path.Combine(roaming, "Slack")]);

        yield return new AppLeftovers(
            "Unity Hub",
            [Path.Combine(programFiles, "Unity Hub"), Path.Combine(programFilesX86, "Unity Hub")],
            [Path.Combine(roaming, "UnityHub"), Path.Combine(local, "unityhub-updater")]);

        yield return new AppLeftovers(
            "Epic Games Launcher",
            [Path.Combine(programFiles, "Epic Games"), Path.Combine(programFilesX86, "Epic Games")],
            [Path.Combine(local, "EpicGamesLauncher")]);
    }

    private static IEnumerable<AppLeftovers> MacApps(IEnvironmentService env)
    {
        var support = Path.Combine(env.HomeDirectory, "Library", "Application Support");
        var caches = Path.Combine(env.HomeDirectory, "Library", "Caches");
        var logs = Path.Combine(env.HomeDirectory, "Library", "Logs");

        yield return new AppLeftovers(
            "Claude Desktop",
            ["/Applications/Claude.app"],
            [Path.Combine(support, "Claude"), Path.Combine(caches, "Claude"), Path.Combine(logs, "Claude")]);

        yield return new AppLeftovers(
            "Docker Desktop",
            ["/Applications/Docker.app"],
            [Path.Combine(support, "Docker Desktop"), Path.Combine(caches, "com.docker.docker")]);

        yield return new AppLeftovers(
            "Discord",
            ["/Applications/Discord.app"],
            [Path.Combine(support, "discord")]);

        yield return new AppLeftovers(
            "Slack",
            ["/Applications/Slack.app"],
            [Path.Combine(support, "Slack")]);
    }
}

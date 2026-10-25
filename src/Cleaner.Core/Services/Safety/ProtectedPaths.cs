using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Utils;

namespace Cleaner.Core.Services;

/// <summary>
/// An installed tool's folder. Only its <paramref name="CacheFolders"/>, which the tool downloads again by
/// itself, may be deleted.
/// </summary>
internal sealed record ProtectedRoot(string Path, string Reason, IReadOnlyList<string> CacheFolders);

/// <summary>The built-in lists behind <see cref="DeletionGuard"/>. None of them depend on PATH.</summary>
internal static class ProtectedPaths
{
    private static readonly string[] SteamCaches =
    [
        "Steam/appcache/httpcache", "Steam/config/htmlcache", "Steam/steamapps/shadercache",
        "Steam/steamapps/downloading", "Steam/steamapps/temp", "Steam/logs", "Steam/dumps",
    ];

    /// <summary>Folders that may never be deleted, nor anything that contains them.</summary>
    public static IEnumerable<string> SystemFolders(IEnvironmentService env)
    {
        var home = env.HomeDirectory;
        yield return home;
        if (Utils.PathComparison.Parent(home) is { } users)
        {
            yield return users;
        }

        foreach (var folder in new[] { "Documents", "Desktop", "Downloads", "Pictures", "Music", "Videos", "Movies" })
        {
            yield return Path.Combine(home, folder);
        }

        if (env.IsWindows)
        {
            yield return env.AppDataDirectory;
            yield return env.LocalAppDataDirectory;
            if (env.WindowsDirectory is { } windows)
            {
                yield return windows;
                yield return Path.Combine(windows, "System32");
                yield return Path.Combine(windows, "SysWOW64");
            }

            foreach (var root in WindowsInstallRoots(env))
            {
                yield return root;
            }

            if (OsPaths.ProgramData(env) is { } programData)
            {
                yield return programData;
            }

            yield break;
        }

        foreach (var system in new[] { "/usr", "/opt", "/etc", "/bin", "/sbin", "/lib", "/lib64", "/var", "/boot", "/System", "/Applications", "/Library", "/private", "/snap" })
        {
            yield return system;
        }

        yield return Path.Combine(home, ".config");
        yield return Path.Combine(home, ".local");
        yield return Path.Combine(home, ".local", "share");
        if (env.IsMacOs)
        {
            yield return Path.Combine(home, "Library");
            yield return Path.Combine(home, "Library", "Application Support");
        }

        // Under sudo HOME may be root's; the invoking user's profile still deserves protection.
        if (OsPaths.Env(env, "SUDO_USER") is { } sudoUser)
        {
            yield return env.IsMacOs ? Path.Combine("/Users", sudoUser) : Path.Combine("/home", sudoUser);
        }
    }

    /// <summary>A project search must not start anywhere under these.</summary>
    public static IEnumerable<string> NonProjectFolders(IEnvironmentService env)
    {
        var home = env.HomeDirectory;
        if (env.IsWindows)
        {
            yield return env.AppDataDirectory;
            yield return env.LocalAppDataDirectory;
            if (env.WindowsDirectory is { } windows)
            {
                yield return windows;
            }

            foreach (var root in WindowsInstallRoots(env))
            {
                yield return root;
            }

            if (OsPaths.ProgramData(env) is { } programData)
            {
                yield return programData;
            }

            yield break;
        }

        foreach (var system in new[] { "/usr", "/opt", "/etc", "/bin", "/sbin", "/lib", "/var", "/System", "/Applications", "/Library", "/snap" })
        {
            yield return system;
        }

        yield return Path.Combine(home, ".config");
        yield return Path.Combine(home, ".local");
        yield return Path.Combine(home, "Library");
    }

    /// <summary>Installed tools and personal data, each with the cache folders that may still be cleared.</summary>
    public static IEnumerable<ProtectedRoot> Installs(IEnvironmentService env)
    {
        var home = env.HomeDirectory;
        var local = env.LocalAppDataDirectory;
        var roaming = env.AppDataDirectory;

        ProtectedRoot Root(string? path, string reason, params string[] cacheFolders) =>
            new(path ?? string.Empty, reason, cacheFolders);

        // Rust: the toolchains and the cargo/rustup proxies live beside their download caches.
        yield return Root(OsPaths.Env(env, "CARGO_HOME") ?? Path.Combine(home, ".cargo"), "Cargo's install folder",
            "registry/cache", "registry/src", "git");
        yield return Root(OsPaths.Env(env, "RUSTUP_HOME") ?? Path.Combine(home, ".rustup"), "rustup's toolchains",
            "downloads", "tmp");

        // Scoop: apps, their persisted data, and the shims every scoop command goes through.
        foreach (var scoop in new[] { OsPaths.Env(env, "SCOOP") ?? Path.Combine(home, "scoop"), OsPaths.Env(env, "SCOOP_GLOBAL") ?? OsPaths.ProgramData(env, "scoop") })
        {
            if (scoop is null)
            {
                continue;
            }

            foreach (var part in new[] { "apps", "persist", "shims", "buckets" })
            {
                yield return Root(Path.Combine(scoop, part), $"Scoop's {part} folder");
            }
        }

        yield return Root(Path.Combine(local, "Microsoft", "WinGet", "Packages"), "WinGet's installed packages");
        yield return Root(Path.Combine(local, "Microsoft", "WinGet", "Links"), "WinGet's command links");

        // Node version managers.
        yield return Root(OsPaths.Env(env, "NVM_HOME"), "nvm's installed Node versions", ".cache");
        yield return Root(OsPaths.Env(env, "NVM_SYMLINK"), "the active nvm Node version");
        yield return Root(Path.Combine(roaming, "nvm"), "nvm's installed Node versions");
        yield return Root(Path.Combine(local, "Author Software", "nvm"), "nvm's installed Node versions", ".cache");
        yield return Root(OsPaths.Env(env, "NVM_DIR") ?? Path.Combine(home, ".nvm"), "nvm's installed Node versions", ".cache");
        yield return Root(OsPaths.Env(env, "VOLTA_HOME") ?? Path.Combine(home, ".volta"), "Volta's installed tools");
        yield return Root(OsPaths.Env(env, "FNM_DIR"), "fnm's installed Node versions");

        // JavaScript runtimes and package managers.
        yield return Root(OsPaths.Env(env, "BUN_INSTALL") ?? Path.Combine(home, ".bun"), "Bun's install folder", "install/cache");
        yield return Root(Path.Combine(home, ".deno", "bin"), "Deno's installed commands");
        yield return Root(OsPaths.Env(env, "PNPM_HOME") ?? PnpmHome(env), "pnpm's install folder", "store");
        yield return Root(Path.Combine(roaming, "npm"), "npm's global packages");

        // .NET, Go, Python.
        yield return Root(Path.Combine(home, ".dotnet"), "the user .NET install", "TelemetryStorageService");
        yield return Root(OsPaths.Env(env, "DOTNET_ROOT"), "the .NET install");
        yield return Root(Path.Combine(FirstGoPath(env) ?? Path.Combine(home, "go"), "bin"), "Go's installed commands");
        yield return Root(OsPaths.Env(env, "GOROOT"), "the Go toolchain");
        foreach (var conda in OsPaths.CondaRoots(env))
        {
            yield return Root(conda, "a conda install", "pkgs");
        }

        yield return Root(OsPaths.Env(env, "PYENV_ROOT") ?? Path.Combine(home, ".pyenv"), "pyenv's Python versions");
        yield return Root(Path.Combine(local, "Programs", "Python"), "the installed Python");
        yield return Root(Path.Combine(local, "Python"), "the Python install manager");
        yield return Root(OsPaths.Env(env, "UV_TOOL_DIR") ?? UvData(env, "tools"), "uv's installed tools");
        yield return Root(OsPaths.Env(env, "UV_PYTHON_INSTALL_DIR") ?? UvData(env, "python"), "uv's installed Pythons");
        foreach (var pipx in new[] { OsPaths.Env(env, "PIPX_HOME"), Path.Combine(home, ".local", "pipx"), Path.Combine(home, ".local", "share", "pipx"), Path.Combine(home, "pipx") })
        {
            yield return Root(pipx, "pipx's installed apps", ".cache", "logs");
        }

        // Generic version managers.
        yield return Root(OsPaths.Env(env, "ASDF_DATA_DIR") ?? Path.Combine(home, ".asdf"), "asdf's installed tools", "downloads", "tmp");
        yield return Root(OsPaths.Env(env, "MISE_DATA_DIR") ?? (env.IsWindows ? Path.Combine(local, "mise") : Path.Combine(home, ".local", "share", "mise")),
            "mise's installed tools", "cache");
        yield return Root(OsPaths.Env(env, "SDKMAN_DIR") ?? Path.Combine(home, ".sdkman"), "SDKMAN's installed SDKs", "archives", "tmp");

        // Android and Gradle hold emulators, keystores, and credentials beside their caches.
        yield return Root(Path.Combine(home, ".android"), "Android settings and emulators", "cache", "build-cache");
        yield return Root(OsPaths.Env(env, "GRADLE_USER_HOME") ?? Path.Combine(home, ".gradle"), "Gradle's home",
            "caches", "wrapper/dists", "daemon");
        yield return Root(Path.Combine(home, ".kube"), "Kubernetes config", "cache", "http-cache");

        // Personal command folders and editor extensions.
        yield return Root(Path.Combine(home, ".local", "bin"), "your installed commands");
        yield return Root(Path.Combine(home, "bin"), "your own scripts");
        foreach (var editor in new[] { ".vscode", ".vscode-insiders", ".vscode-oss", ".cursor", ".windsurf" })
        {
            yield return Root(Path.Combine(home, editor, "extensions"), "installed editor extensions");
        }

        foreach (var server in new[] { ".vscode-server", ".vscode-server-insiders", ".cursor-server" })
        {
            yield return Root(Path.Combine(home, server), "an editor's remote server");
        }

        yield return Root(env.DataDirectory, "Cleaner's own files");
        foreach (var personal in new[] { ".ssh", ".gnupg", ".aws", ".azure", ".docker" })
        {
            yield return Root(Path.Combine(home, personal), "personal configuration");
        }

        if (env.IsWindows)
        {
            yield return Root(Path.Combine(local, "Programs"), "per-user installed programs");
            foreach (var programFiles in WindowsInstallRoots(env))
            {
                yield return Root(programFiles, "installed programs", SteamCaches);
            }
        }
        else
        {
            yield return Root("/Applications", "installed applications");
            yield return Root(Path.Combine(home, "Applications"), "installed applications");
            yield return Root("/opt/homebrew", "Homebrew's installed packages");
            yield return Root("/usr/local/Cellar", "Homebrew's installed packages");
            yield return Root("/home/linuxbrew/.linuxbrew", "Homebrew's installed packages");
        }
    }

    private static IEnumerable<string> WindowsInstallRoots(IEnvironmentService env)
    {
        foreach (var root in new[] { OsPaths.ProgramFiles(env), OsPaths.ProgramFiles(env, x86: true), OsPaths.Env(env, "ProgramW6432") })
        {
            if (root is not null)
            {
                yield return root;
            }
        }
    }

    private static string PnpmHome(IEnvironmentService env) =>
        env.IsWindows ? Path.Combine(env.LocalAppDataDirectory, "pnpm")
        : env.IsMacOs ? Path.Combine(env.HomeDirectory, "Library", "pnpm")
        : Path.Combine(env.HomeDirectory, ".local", "share", "pnpm");

    private static string UvData(IEnvironmentService env, string folder) =>
        env.IsWindows
            ? Path.Combine(env.AppDataDirectory, "uv", folder)
            : Path.Combine(OsPaths.Env(env, "XDG_DATA_HOME") ?? Path.Combine(env.HomeDirectory, ".local", "share"), "uv", folder);

    /// <summary>GOPATH may list several workspaces; installed commands go to the first.</summary>
    private static string? FirstGoPath(IEnvironmentService env) =>
        OsPaths.Env(env, "GOPATH")?.Split(env.IsWindows ? ';' : ':', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
}

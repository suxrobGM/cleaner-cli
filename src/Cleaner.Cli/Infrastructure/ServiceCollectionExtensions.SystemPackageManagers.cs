using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Cleaners.Os;
using Cleaner.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Cleaner.Cli.Infrastructure;

internal static partial class ServiceCollectionExtensions
{
    /// <summary>Registers system package managers and their platform-specific cache paths.</summary>
    private static void AddSystemPackageManagerCleaners(this IServiceCollection services)
    {
        services.AddSingleton<ICleaner>(_ => new SystemPackageManagerCleaner(
            "apt", "APT cache", "apt-get", ["clean"], requiresElevation: true,
            env => env.IsLinux,
            _ => [new CleanupPath("/var/cache/apt/archives", DeleteMode.ClearContents)]));

        services.AddSingleton<ICleaner>(_ => new SystemPackageManagerCleaner(
            "dnf", "DNF cache", "dnf", ["clean", "all"], requiresElevation: true,
            env => env.IsLinux));

        services.AddSingleton<ICleaner>(_ => new SystemPackageManagerCleaner(
            "pacman", "Pacman cache", "pacman", ["-Sc", "--noconfirm"], requiresElevation: true,
            env => env.IsLinux));

        services.AddSingleton<ICleaner>(_ => new SystemPackageManagerCleaner(
            "brew", "Homebrew cache", "brew", ["cleanup", "-s"], requiresElevation: false,
            env => env.IsMacOs || env.IsLinux,
            ctx =>
            [
                new CleanupPath(ctx.Environment.IsMacOs
                    ? Path.Combine(ctx.Environment.HomeDirectory, "Library", "Caches", "Homebrew")
                    : Path.Combine(ctx.Environment.CacheDirectory, "Homebrew"), DeleteMode.ClearContents),
            ]));

        // Scoop knows which app version is current, so old versions are left to `scoop cleanup`;
        // the cache directory is the only thing ever deleted directly.
        services.AddSingleton<ICleaner>(_ => new SystemPackageManagerCleaner(
            "scoop", "Scoop cache and old versions", "scoop", ["cache", "rm", "*"], requiresElevation: false,
            env => env.IsWindows,
            ctx => [new CleanupPath(ScoopCacheDirectory(ctx.Environment), DeleteMode.ClearContents)],
            followUpArguments: [["cleanup", "*"]],
            confirmationWarning:
                "this also removes the old app versions Scoop keeps for rollback, so " +
                "'scoop reset app@version' can no longer switch back to them"));

        services.AddSingleton<ICleaner>(_ => new SystemPackageManagerCleaner(
            "choco", "Chocolatey cache", "choco", ["cache", "remove"], requiresElevation: true,
            env => env.IsWindows,
            ctx => [new CleanupPath(Path.Combine(ctx.Environment.TempDirectory, "chocolatey"), DeleteMode.ClearContents)]));

        services.AddSingleton<ICleaner>(_ => new SystemPackageManagerCleaner(
            "flatpak", "Flatpak unused runtimes", "flatpak", ["uninstall", "--unused", "--assumeyes"],
            requiresElevation: false,
            env => env.IsLinux));

        // No -d: unreachable store paths only, keeping rollback generations intact.
        services.AddSingleton<ICleaner>(_ => new SystemPackageManagerCleaner(
            "nix", "Nix store garbage", "nix-collect-garbage", [], requiresElevation: false,
            env => env.IsLinux || env.IsMacOs));

        services.AddSingleton<ICleaner, WingetCleaner>();
    }

    /// <summary>Scoop's download cache: <c>SCOOP_CACHE</c>, else <c>$SCOOP\cache</c>, else <c>~\scoop\cache</c>.</summary>
    private static string ScoopCacheDirectory(IEnvironmentService env)
    {
        if (env.GetEnvironmentVariable("SCOOP_CACHE") is { } cache && !string.IsNullOrWhiteSpace(cache))
        {
            return cache;
        }

        var root = env.GetEnvironmentVariable("SCOOP");
        return Path.Combine(string.IsNullOrWhiteSpace(root) ? env.HomePath("scoop") : root, "cache");
    }
}

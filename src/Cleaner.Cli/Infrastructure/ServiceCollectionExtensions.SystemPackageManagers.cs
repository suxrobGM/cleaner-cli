using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Cleaners.Os;
using Microsoft.Extensions.DependencyInjection;

namespace Cleaner.Cli.Infrastructure;

internal static partial class ServiceCollectionExtensions
{
    /// <summary>
    /// System package managers. Each is a <see cref="SystemPackageManagerCleaner"/> configured with
    /// its executable, clean arguments, OS predicate, and (optionally) the cache paths to size/delete.
    /// </summary>
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

        services.AddSingleton<ICleaner>(_ => new SystemPackageManagerCleaner(
            "scoop", "Scoop cache", "scoop", ["cache", "rm", "*"], requiresElevation: false,
            env => env.IsWindows,
            ctx => [new CleanupPath(ctx.Environment.HomePath("scoop", "cache"), DeleteMode.ClearContents)]));

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
}

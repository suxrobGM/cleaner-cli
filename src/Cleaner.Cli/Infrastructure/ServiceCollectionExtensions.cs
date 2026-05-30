using Cleaner.Cli.Application;
using Cleaner.Cli.Commands;
using Cleaner.Cli.Rendering;
using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Applications;
using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Cleaners.DevTools;
using Cleaner.Core.Cleaners.Os;
using Cleaner.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace Cleaner.Cli.Infrastructure;

/// <summary>
/// The composition root. Everything is registered explicitly with factory lambdas — no assembly
/// scanning and no reflection-based activation — so the app stays Native-AOT clean.
/// </summary>
internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCleaner(this IServiceCollection services)
    {
        services.AddSingleton<IAnsiConsole>(_ => AnsiConsole.Console);
        services.AddSingleton<IEnvironmentService, EnvironmentService>();
        services.AddSingleton<IFileSystemService, FileSystemService>();
        services.AddSingleton<IProcessRunner, ProcessRunner>(_ => new ProcessRunner());
        services.AddSingleton<IGitHubReleaseClient, GitHubReleaseClient>();
        services.AddSingleton<IUpdateService, UpdateService>();

        services.AddCleaners();

        services.AddSingleton<ICleanerRegistry, CleanerRegistry>();
        services.AddSingleton<IConsoleRenderer, ConsoleRenderer>();
        services.AddSingleton<CleanupContextFactory>();
        services.AddSingleton<CleanerApp>();
        services.AddSingleton<CommandLineBuilder>();

        return services;
    }

    /// <summary>
    /// Registers every cleaner. Adding a new cleaner is one line here — keep them grouped by the
    /// same categories used for display.
    /// </summary>
    private static void AddCleaners(this IServiceCollection services)
    {
        // .NET
        services.AddSingleton<ICleaner, NuGetCleaner>();
        services.AddSingleton<ICleaner, DotnetCleaner>();

        // JavaScript / TypeScript
        services.AddSingleton<ICleaner, NpmCleaner>();
        services.AddSingleton<ICleaner, NpxCleaner>();
        services.AddSingleton<ICleaner, YarnCleaner>();
        services.AddSingleton<ICleaner, PnpmCleaner>();
        services.AddSingleton<ICleaner, BunCleaner>();
        services.AddSingleton<ICleaner, DenoCleaner>();

        // Python
        services.AddSingleton<ICleaner, PipCleaner>();
        services.AddSingleton<ICleaner, PipenvCleaner>();
        services.AddSingleton<ICleaner, PoetryCleaner>();
        services.AddSingleton<ICleaner, CondaCleaner>();
        services.AddSingleton<ICleaner, PdmCleaner>();
        services.AddSingleton<ICleaner, UvCleaner>();

        // Rust
        services.AddSingleton<ICleaner, CargoCleaner>();
        services.AddSingleton<ICleaner, RustupCleaner>();
        services.AddSingleton<ICleaner, SccacheCleaner>();

        // Go
        services.AddSingleton<ICleaner, GoCleaner>();

        // JVM / Android
        services.AddSingleton<ICleaner, GradleCleaner>();
        services.AddSingleton<ICleaner, MavenCleaner>();
        services.AddSingleton<ICleaner, SbtIvyCleaner>();
        services.AddSingleton<ICleaner, AndroidSdkCleaner>();

        // Mobile (React Native / Expo)
        services.AddSingleton<ICleaner, ReactNativeCleaner>();
        services.AddSingleton<ICleaner, ExpoCleaner>();

        // Other languages
        services.AddSingleton<ICleaner, GemBundlerCleaner>();
        services.AddSingleton<ICleaner, ComposerCleaner>();
        services.AddSingleton<ICleaner, PubCleaner>();
        services.AddSingleton<ICleaner, HexMixCleaner>();
        services.AddSingleton<ICleaner, CabalStackCleaner>();

        // Build / monorepo caches
        services.AddSingleton<ICleaner, CcacheCleaner>();
        services.AddSingleton<ICleaner, BazelCleaner>();
        services.AddSingleton<ICleaner, TurboNxCleaner>();
        services.AddSingleton<ICleaner, NodeModulesCacheCleaner>();

        // Containers / IaC
        services.AddSingleton<ICleaner, DockerCleaner>();
        services.AddSingleton<ICleaner, TerraformCleaner>();

        // IDEs / editors
        services.AddSingleton<ICleaner, JetBrainsCleaner>();
        services.AddSingleton<ICleaner, VsCodeCleaner>();
        services.AddSingleton<ICleaner, VisualStudioCleaner>();
        services.AddSingleton<ICleaner, XcodeCleaner>();

        // Tooling downloads
        services.AddSingleton<ICleaner, BrowserAutomationCleaner>();
        services.AddSingleton<ICleaner, ElectronCacheCleaner>();

        // Project-local
        services.AddSingleton<ICleaner, BuildArtifactCleaner>();

        // Operating system
        services.AddSingleton<ICleaner, UserTempCleaner>();
        services.AddSingleton<ICleaner, TrashCleaner>();
        services.AddSingleton<ICleaner, BrowserCacheCleaner>();
        services.AddSingleton<ICleaner, WindowsUpdateCacheCleaner>();
        services.AddSingleton<ICleaner, WindowsTempCleaner>();
        services.AddSingleton<ICleaner, ThumbnailCacheCleaner>();
        services.AddSingleton<ICleaner, CrashDumpCleaner>();
        services.AddSingleton<ICleaner, DeliveryOptimizationCleaner>();
        services.AddSingleton<ICleaner, WindowsLogCleaner>();
        services.AddSingleton<ICleaner, ServiceProfileTempCleaner>();
        services.AddSingleton<ICleaner, DownloadedProgramFilesCleaner>();
        services.AddSingleton<ICleaner, SystemMemoryDumpCleaner>();
        services.AddSingleton<ICleaner, MacUserCachesCleaner>();
        services.AddSingleton<ICleaner, XdgCacheCleaner>();
        services.AddSingleton<ICleaner, JournalLogCleaner>();

        // System package managers
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

        // Applications
        services.AddSingleton<ICleaner, SteamCleaner>();
    }
}

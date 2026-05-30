using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.DevTools;
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
        services.AddSingleton<IEnvironmentService>(_ => new EnvironmentService());
        services.AddSingleton<IFileSystemService>(_ => new FileSystemService());
        services.AddSingleton<IProcessRunner>(_ => new ProcessRunner());

        services.AddCleaners();

        services.AddSingleton<ICleanerRegistry>(sp => new CleanerRegistry(sp.GetServices<ICleaner>()));
        services.AddSingleton<CleanerApp>(sp => new CleanerApp(
            sp.GetRequiredService<ICleanerRegistry>(),
            sp.GetRequiredService<IAnsiConsole>(),
            sp.GetRequiredService<IEnvironmentService>(),
            sp.GetRequiredService<IFileSystemService>(),
            sp.GetRequiredService<IProcessRunner>()));

        return services;
    }

    /// <summary>
    /// Registers every cleaner. Adding a new cleaner is one line here — keep them grouped by the
    /// same categories used for display.
    /// </summary>
    private static void AddCleaners(this IServiceCollection services)
    {
        // .NET
        services.AddSingleton<ICleaner>(_ => new NuGetCleaner());
        services.AddSingleton<ICleaner>(_ => new DotnetCleaner());

        // JavaScript / TypeScript
        services.AddSingleton<ICleaner>(_ => new NpmCleaner());
        services.AddSingleton<ICleaner>(_ => new NpxCleaner());
        services.AddSingleton<ICleaner>(_ => new YarnCleaner());
        services.AddSingleton<ICleaner>(_ => new PnpmCleaner());
        services.AddSingleton<ICleaner>(_ => new BunCleaner());
        services.AddSingleton<ICleaner>(_ => new DenoCleaner());

        // Python
        services.AddSingleton<ICleaner>(_ => new PipCleaner());
        services.AddSingleton<ICleaner>(_ => new PipenvCleaner());
        services.AddSingleton<ICleaner>(_ => new PoetryCleaner());
        services.AddSingleton<ICleaner>(_ => new CondaCleaner());
        services.AddSingleton<ICleaner>(_ => new PdmCleaner());
        services.AddSingleton<ICleaner>(_ => new UvCleaner());

        // Rust
        services.AddSingleton<ICleaner>(_ => new CargoCleaner());
        services.AddSingleton<ICleaner>(_ => new RustupCleaner());
        services.AddSingleton<ICleaner>(_ => new SccacheCleaner());

        // Go
        services.AddSingleton<ICleaner>(_ => new GoCleaner());

        // JVM / Android
        services.AddSingleton<ICleaner>(_ => new GradleCleaner());
        services.AddSingleton<ICleaner>(_ => new MavenCleaner());
        services.AddSingleton<ICleaner>(_ => new SbtIvyCleaner());
        services.AddSingleton<ICleaner>(_ => new AndroidSdkCleaner());

        // Mobile (React Native / Expo)
        services.AddSingleton<ICleaner>(_ => new ReactNativeCleaner());
        services.AddSingleton<ICleaner>(_ => new ExpoCleaner());

        // Other languages
        services.AddSingleton<ICleaner>(_ => new GemBundlerCleaner());
        services.AddSingleton<ICleaner>(_ => new ComposerCleaner());
        services.AddSingleton<ICleaner>(_ => new PubCleaner());
        services.AddSingleton<ICleaner>(_ => new HexMixCleaner());
        services.AddSingleton<ICleaner>(_ => new CabalStackCleaner());

        // Build / monorepo caches
        services.AddSingleton<ICleaner>(_ => new CcacheCleaner());
        services.AddSingleton<ICleaner>(_ => new BazelCleaner());
        services.AddSingleton<ICleaner>(_ => new TurboNxCleaner());
        services.AddSingleton<ICleaner>(_ => new NodeModulesCacheCleaner());

        // Containers / IaC
        services.AddSingleton<ICleaner>(_ => new DockerCleaner());
        services.AddSingleton<ICleaner>(_ => new TerraformCleaner());

        // IDEs / editors
        services.AddSingleton<ICleaner>(_ => new JetBrainsCleaner());
        services.AddSingleton<ICleaner>(_ => new VsCodeCleaner());
        services.AddSingleton<ICleaner>(_ => new VisualStudioCleaner());
        services.AddSingleton<ICleaner>(_ => new XcodeCleaner());

        // Tooling downloads
        services.AddSingleton<ICleaner>(_ => new BrowserAutomationCleaner());
        services.AddSingleton<ICleaner>(_ => new ElectronCacheCleaner());

        // Project-local
        services.AddSingleton<ICleaner>(_ => new BuildArtifactCleaner());
    }
}

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
    }
}

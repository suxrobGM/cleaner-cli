using Cleaner.Cli.Application;
using Cleaner.Cli.Commands;
using Cleaner.Cli.Rendering;
using Cleaner.Core.Abstractions;
using Cleaner.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace Cleaner.Cli.Infrastructure;

/// <summary>Explicit Native-AOT-safe composition root; cleaner categories are split into partials.</summary>
internal static partial class ServiceCollectionExtensions
{
    public static IServiceCollection AddCleaner(this IServiceCollection services)
    {
        services.AddSingleton<IAnsiConsole>(_ => AnsiConsole.Console);
        services.AddSingleton<IAppLogger, SerilogAppLogger>();
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

    /// <summary>Registers cleaners from the category-specific partial methods.</summary>
    private static void AddCleaners(this IServiceCollection services)
    {
        services.AddDevToolCleaners();
        services.AddOsCleaners();
        services.AddSystemPackageManagerCleaners();
        services.AddApplicationCleaners();
    }

}

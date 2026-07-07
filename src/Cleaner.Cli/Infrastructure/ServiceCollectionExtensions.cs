using Cleaner.Cli.Application;
using Cleaner.Cli.Commands;
using Cleaner.Cli.Rendering;
using Cleaner.Core.Abstractions;
using Cleaner.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

namespace Cleaner.Cli.Infrastructure;

/// <summary>
/// The composition root. Everything is registered explicitly with factory lambdas — no assembly
/// scanning and no reflection-based activation — so the app stays Native-AOT clean. The per-category
/// cleaner registrations live in the <c>ServiceCollectionExtensions.*.cs</c> partial files.
/// </summary>
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

    /// <summary>
    /// Registers every cleaner, delegating to the per-category partials. Adding a new cleaner is one
    /// line in the matching <c>AddXCleaners</c> method.
    /// </summary>
    private static void AddCleaners(this IServiceCollection services)
    {
        services.AddDevToolCleaners();
        services.AddOsCleaners();
        services.AddSystemPackageManagerCleaners();
        services.AddApplicationCleaners();
    }

}

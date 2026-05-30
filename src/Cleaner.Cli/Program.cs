using Cleaner.Cli.Commands;
using Cleaner.Cli.Infrastructure;
using Cleaner.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

var services = new ServiceCollection();
services.AddCleaner();
using var provider = services.BuildServiceProvider();

var logger = provider.GetRequiredService<IAppLogger>();
var updateService = provider.GetRequiredService<IUpdateService>();

// Last-resort capture: anything that escapes the normal flow is recorded before the process dies.
AppDomain.CurrentDomain.UnhandledException += (_, e) =>
    logger.Error("Unhandled exception", e.ExceptionObject as Exception);

TaskScheduler.UnobservedTaskException += (_, e) =>
{
    logger.Error("Unobserved task exception", e.Exception);
    e.SetObserved();
};

// Remove any leftover binary backup from a previous self-update (Windows renames the old exe aside).
updateService.CleanupStaleBackup();

try
{
    var builder = provider.GetRequiredService<CommandLineBuilder>();
    return await builder.Build().Parse(args).InvokeAsync();
}
catch (Exception ex)
{
    logger.Error("Cleaner terminated with an unhandled exception", ex);
    AnsiConsole.MarkupLine("[red]Cleaner hit an unexpected error and had to stop.[/]");
    AnsiConsole.MarkupLine($"[grey]Details written to the log: {logger.LogFilePath.EscapeMarkup()}[/]");
    return 1;
}

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

// Log anything that escapes the normal flow before the process dies.
AppDomain.CurrentDomain.UnhandledException += (_, e) =>
    logger.Error("Unhandled exception", e.ExceptionObject as Exception);

TaskScheduler.UnobservedTaskException += (_, e) =>
{
    logger.Error("Unobserved task exception", e.Exception);
    e.SetObserved();
};

// Self-update on Windows renames the old exe aside; remove that backup.
updateService.CleanupStaleBackup();

try
{
    var builder = provider.GetRequiredService<CommandLineBuilder>();
    return await builder.Build().Parse(args).InvokeAsync();
}
catch (OperationCanceledException)
{
    // User-initiated Ctrl+C is not an error; 130 is the conventional SIGINT exit code.
    AnsiConsole.MarkupLine("[grey]Cancelled.[/]");
    return 130;
}
catch (Exception ex)
{
    logger.Error("Cleaner terminated with an unhandled exception", ex);
    AnsiConsole.MarkupLine("[red]Cleaner hit an unexpected error and had to stop.[/]");
    AnsiConsole.MarkupLine($"[grey]Details written to the log: {logger.LogFilePath.EscapeMarkup()}[/]");
    return 1;
}

using Cleaner.Cli.Commands;
using Cleaner.Cli.Infrastructure;
using Cleaner.Core.Services;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddCleaner();
using var provider = services.BuildServiceProvider();

// Remove any leftover binary backup from a previous self-update (Windows renames the old exe aside).
provider.GetRequiredService<IUpdateService>().CleanupStaleBackup();

var builder = provider.GetRequiredService<CommandLineBuilder>();
return await builder.Build().Parse(args).InvokeAsync();

using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Applications;
using Microsoft.Extensions.DependencyInjection;

namespace Cleaner.Cli.Infrastructure;

internal static partial class ServiceCollectionExtensions
{
    /// <summary>Cleaners for large desktop apps: Steam, Electron apps, media/sync clients, etc.</summary>
    private static void AddApplicationCleaners(this IServiceCollection services)
    {
        services.AddSingleton<ICleaner, SteamCleaner>();
        services.AddSingleton<ICleaner, ElectronAppCacheCleaner>();
        services.AddSingleton<ICleaner, SpotifyCleaner>();
        services.AddSingleton<ICleaner, TelegramCleaner>();
        services.AddSingleton<ICleaner, GameLauncherCleaner>();
        services.AddSingleton<ICleaner, AdobeMediaCacheCleaner>();
        services.AddSingleton<ICleaner, OneDriveCleaner>();
        services.AddSingleton<ICleaner, DropboxCleaner>();
    }
}

using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Os;
using Microsoft.Extensions.DependencyInjection;

namespace Cleaner.Cli.Infrastructure;

internal static partial class ServiceCollectionExtensions
{
    /// <summary>Operating-system cleaners: temp/trash, browser caches, and Windows-specific caches.</summary>
    private static void AddOsCleaners(this IServiceCollection services)
    {
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
        services.AddSingleton<ICleaner, GpuShaderCacheCleaner>();
        services.AddSingleton<ICleaner, InetCacheCleaner>();
        services.AddSingleton<ICleaner, StoreAppCacheCleaner>();
        services.AddSingleton<ICleaner, MacUserCachesCleaner>();
        services.AddSingleton<ICleaner, XdgCacheCleaner>();
        services.AddSingleton<ICleaner, JournalLogCleaner>();
        services.AddSingleton<ICleaner, GpuInstallerLeftoverCleaner>();
        services.AddSingleton<ICleaner, WinSxSCleaner>();
        services.AddSingleton<ICleaner, WindowsOldCleaner>();
    }
}

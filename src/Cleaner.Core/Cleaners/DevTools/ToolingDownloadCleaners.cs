using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>Headless-browser downloads from Playwright, Puppeteer, and Cypress.</summary>
public sealed class BrowserAutomationCleaner : DirectoryCleanerBase
{
    public override string Id => "browser-automation";

    public override string Name => "Playwright / Puppeteer / Cypress";

    public override string Category => Categories.ToolingDownloads;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return new CleanupPath(OsPaths.AppCache(env, "ms-playwright", "ms-playwright", "ms-playwright"), Description: "Playwright browsers");
        yield return new CleanupPath(OsPaths.AppCache(env, "puppeteer", "puppeteer", "puppeteer"), Description: "Puppeteer browsers");
        yield return new CleanupPath(OsPaths.AppCache(env, Path.Combine("Cypress", "Cache"), "Cypress", "Cypress"), Description: "Cypress binaries");

        // Puppeteer and Playwright default to ~/.cache for downloaded browsers even on Windows/macOS;
        // ExistingTargets de-dupes where this coincides with the cache dir above (Linux).
        yield return new CleanupPath(env.HomePath(".cache", "puppeteer"), Description: "Puppeteer browsers");
        yield return new CleanupPath(env.HomePath(".cache", "ms-playwright"), Description: "Playwright browsers");
    }
}

/// <summary>Electron and electron-builder download caches.</summary>
public sealed class ElectronCacheCleaner : DirectoryCleanerBase
{
    public override string Id => "electron";

    public override string Name => "Electron caches";

    public override string Category => Categories.ToolingDownloads;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return new CleanupPath(OsPaths.AppCache(env, Path.Combine("electron", "Cache"), "electron", "electron"), Description: "Electron");
        yield return new CleanupPath(OsPaths.AppCache(env, Path.Combine("electron-builder", "Cache"), "electron-builder", "electron-builder"), Description: "electron-builder");
    }
}

/// <summary>Azure Functions Core Tools downloaded runtime feeds (re-fetched on demand).</summary>
public sealed class AzureFunctionsToolsCleaner : DirectoryCleanerBase
{
    public override string Id => "azure-functions";

    public override string Name => "Azure Functions Core Tools downloads";

    public override string Category => Categories.ToolingDownloads;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        // The Visual Studio / VS Code tooling stores release feeds under %LOCALAPPDATA% on Windows.
        yield return new CleanupPath(
            Path.Combine(context.Environment.LocalAppDataDirectory, "AzureFunctionsTools", "Releases"),
            DeleteMode.ClearContents);
    }
}

/// <summary>DotSlash fetched-executable cache.</summary>
public sealed class DotslashCleaner : DirectoryCleanerBase
{
    public override string Id => "dotslash";

    public override string Name => "DotSlash cache";

    public override string Category => Categories.ToolingDownloads;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        [new CleanupPath(OsPaths.AppCache(context.Environment, "dotslash", "dotslash", "dotslash"), DeleteMode.ClearContents)];
}

/// <summary>Google Cloud CLI logs and surface caches; config and credentials are never touched.</summary>
public sealed class GcloudCleaner : DirectoryCleanerBase
{
    public override string Id => "gcloud";

    public override string Name => "gcloud logs & cache";

    public override string Category => Categories.ToolingDownloads;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        var config = OsPaths.Env(env, "CLOUDSDK_CONFIG")
            ?? (env.IsWindows
                ? Path.Combine(env.AppDataDirectory, "gcloud")
                : Path.Combine(env.HomeDirectory, ".config", "gcloud"));

        yield return new CleanupPath(Path.Combine(config, "logs"), DeleteMode.ClearContents, "logs");
        yield return new CleanupPath(Path.Combine(config, "surface_data"), DeleteMode.ClearContents, "surface cache");
    }
}

/// <summary>SonarLint / sonar-scanner plugin and analyzer cache.</summary>
public sealed class SonarCleaner : DirectoryCleanerBase
{
    public override string Id => "sonar";

    public override string Name => "Sonar scanner cache";

    public override string Category => Categories.ToolingDownloads;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        var root = OsPaths.Env(env, "SONAR_USER_HOME") ?? env.HomePath(".sonar");
        yield return new CleanupPath(Path.Combine(root, "cache"), DeleteMode.ClearContents);
    }
}

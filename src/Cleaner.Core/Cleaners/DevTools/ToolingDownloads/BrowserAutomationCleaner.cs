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

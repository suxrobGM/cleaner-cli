using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// Headless-browser downloads from Playwright, Puppeteer, and Cypress. The Playwright MCP folder
/// (<c>ms-playwright-mcp</c>) is kept: it holds browser profiles with logins and cookies.
/// </summary>
public sealed class BrowserAutomationCleaner : DirectoryCleanerBase
{
    public override string Id => "browser-automation";

    public override string Name => "Playwright / Puppeteer / Cypress";

    public override string Category => Categories.ToolingDownloads;

    public override string ConfirmationWarning =>
        "these are the browsers Playwright, Puppeteer and Cypress run tests with, and tests fail until " +
        "they are reinstalled — 'npx playwright install', 'npx cypress install', or reinstalling Puppeteer";

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

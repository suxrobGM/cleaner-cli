using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

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

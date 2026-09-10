using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Services;

namespace Cleaner.Core.Cleaners.Os;

/// <summary>Temp directories of the built-in service accounts (LocalService, NetworkService). Needs elevation.</summary>
public sealed class ServiceProfileTempCleaner : WindowsCleanerBase
{
    private static readonly string[] ServiceAccounts = ["LocalService", "NetworkService"];

    public override string Id => "service-temp";

    public override string Name => "Service account temp";

    public override bool RequiresElevation => true;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var windows = context.Environment.WindowsDirectory;
        if (windows is null)
        {
            yield break;
        }

        foreach (var account in ServiceAccounts)
        {
            yield return new CleanupPath(
                Path.Combine(windows, "ServiceProfiles", account, "AppData", "Local", "Temp"),
                DeleteMode.ClearContents,
                account);
        }
    }
}

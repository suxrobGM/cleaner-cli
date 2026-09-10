using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Services;

namespace Cleaner.Core.Cleaners.Os;

/// <summary>WinINet / "Temporary Internet Files" cache used by IE, legacy Edge, and WebView hosts.</summary>
public sealed class InetCacheCleaner : WindowsCleanerBase
{
    public override string Id => "inet-cache";

    public override string Name => "Temporary Internet Files";

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        yield return new CleanupPath(
            Path.Combine(context.Environment.LocalAppDataDirectory, "Microsoft", "Windows", "INetCache"),
            DeleteMode.ClearContents);
    }
}

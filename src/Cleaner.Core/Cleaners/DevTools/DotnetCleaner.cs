using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>Clears .NET SDK caches (template engine). NuGet is handled by its own cleaner.</summary>
public sealed class DotnetCleaner : DirectoryCleanerBase
{
    public override string Id => "dotnet";

    public override string Name => ".NET SDK caches";

    public override string Category => Categories.PackageManagers;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return new CleanupPath(env.HomePath(".templateengine"), Description: "template engine cache");
        yield return new CleanupPath(env.HomePath(".dotnet", "TelemetryStorageService"), Description: "telemetry cache");
    }
}

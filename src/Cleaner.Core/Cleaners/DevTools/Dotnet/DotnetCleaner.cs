using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// Clears the .NET SDK telemetry cache. <c>~/.templateengine</c> is kept: it holds the template
/// packages installed with <c>dotnet new install</c>. NuGet is handled by its own cleaner.
/// </summary>
public sealed class DotnetCleaner : DirectoryCleanerBase
{
    public override string Id => "dotnet";

    public override string Name => ".NET SDK caches";

    public override string Category => Categories.Dotnet;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        [new CleanupPath(context.Environment.HomePath(".dotnet", "TelemetryStorageService"), Description: "telemetry cache")];
}

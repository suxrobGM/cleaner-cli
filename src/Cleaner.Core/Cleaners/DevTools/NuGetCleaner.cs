using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// Clears NuGet caches. Prefers <c>dotnet nuget locals all --clear</c> when the .NET SDK is present;
/// otherwise deletes the cache directories directly.
/// </summary>
public sealed class NuGetCleaner : ProcessCleanerBase
{
    public override string Id => "nuget";

    public override string Name => "NuGet caches";

    public override string Category => Categories.PackageManagers;

    protected override string Executable => "dotnet";

    protected override IReadOnlyList<string> CleanArguments => ["nuget", "locals", "all", "--clear"];

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;

        // global-packages — the big one, same location on every OS.
        yield return new CleanupPath(env.HomePath(".nuget", "packages"), Description: "global-packages");

        if (env.IsWindows)
        {
            var local = env.LocalAppDataDirectory;
            yield return new CleanupPath(Path.Combine(local, "NuGet", "v3-cache"), Description: "http-cache");
            yield return new CleanupPath(Path.Combine(local, "NuGet", "plugins-cache"), Description: "plugins-cache");
            yield return new CleanupPath(Path.Combine(local, "Temp", "NuGetScratch"), Description: "temp");
        }
        else
        {
            // XDG-style locations used by NuGet on macOS/Linux.
            var data = Path.Combine(env.HomeDirectory, ".local", "share", "NuGet");
            yield return new CleanupPath(Path.Combine(data, "v3-cache"), Description: "http-cache");
            yield return new CleanupPath(Path.Combine(data, "plugins-cache"), Description: "plugins-cache");
            yield return new CleanupPath(Path.Combine(env.TempDirectory, "NuGetScratch"), Description: "temp");
        }
    }
}

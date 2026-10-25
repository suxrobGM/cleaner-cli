using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>NuGet caches. Prefers <c>dotnet nuget locals all --clear</c> when the .NET SDK is installed.</summary>
public sealed class NuGetCleaner : ProcessCleanerBase
{
    public override string Id => "nuget";

    public override string Name => "NuGet caches";

    public override string Category => Categories.Dotnet;

    protected override string Executable => "dotnet";

    protected override IReadOnlyList<string> CleanArguments => ["nuget", "locals", "all", "--clear"];

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;

        // global-packages is the same on every OS unless NUGET_PACKAGES moves it.
        yield return new CleanupPath(
            OsPaths.Env(env, "NUGET_PACKAGES") ?? env.HomePath(".nuget", "packages"),
            Description: "global-packages");

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

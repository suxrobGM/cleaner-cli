using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// Unreal Engine's shared DerivedDataCache for regenerable shaders and asset derivations. Projects,
/// plugins, and engine installs are preserved.
/// </summary>
public sealed class UnrealCleaner : DirectoryCleanerBase
{
    public override string Id => "unreal";

    public override string Name => "Unreal Engine DerivedDataCache";

    public override string Category => Categories.GameDev;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;

        // UE-LOCALDATACACHEPATH / the shared DDC env override, when set.
        if (OsPaths.Env(env, "UE-LocalDataCachePath", "UE_LocalDataCachePath") is { } configured)
        {
            yield return new CleanupPath(configured, DeleteMode.ClearContents);
        }

        yield return env.IsWindows
            ? new CleanupPath(
                Path.Combine(env.LocalAppDataDirectory, "UnrealEngine", "Common", "DerivedDataCache"),
                DeleteMode.ClearContents)
            : new CleanupPath(
                Path.Combine(env.HomeDirectory, "Library", "Application Support", "Epic", "UnrealEngine", "Common", "DerivedDataCache"),
                DeleteMode.ClearContents);
    }
}

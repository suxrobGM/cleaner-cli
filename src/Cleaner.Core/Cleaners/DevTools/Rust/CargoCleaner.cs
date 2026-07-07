using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>Cargo registry caches and git sources (keeps installed binaries in ~/.cargo/bin).</summary>
public sealed class CargoCleaner : DirectoryCleanerBase
{
    public override string Id => "cargo";

    public override string Name => "Cargo registry cache";

    public override string Category => Categories.Rust;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        var cargoHome = OsPaths.Env(env, "CARGO_HOME") ?? env.HomePath(".cargo");
        yield return new CleanupPath(Path.Combine(cargoHome, "registry", "cache"), Description: "registry cache");
        yield return new CleanupPath(Path.Combine(cargoHome, "registry", "src"), Description: "extracted sources");
        yield return new CleanupPath(Path.Combine(cargoHome, "git"), Description: "git sources");
    }
}

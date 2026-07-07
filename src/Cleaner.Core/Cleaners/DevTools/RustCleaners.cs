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

/// <summary>rustup toolchain download/temp caches.</summary>
public sealed class RustupCleaner : DirectoryCleanerBase
{
    public override string Id => "rustup";

    public override string Name => "rustup downloads";

    public override string Category => Categories.Rust;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        var rustupHome = OsPaths.Env(env, "RUSTUP_HOME") ?? env.HomePath(".rustup");
        yield return new CleanupPath(Path.Combine(rustupHome, "downloads"));
        yield return new CleanupPath(Path.Combine(rustupHome, "tmp"));
    }
}

/// <summary>sccache compilation cache.</summary>
public sealed class SccacheCleaner : DirectoryCleanerBase
{
    public override string Id => "sccache";

    public override string Name => "sccache";

    public override string Category => Categories.Rust;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        [new CleanupPath(OsPaths.AppCache(context.Environment, Path.Combine("Mozilla", "sccache"), "Mozilla.sccache", "sccache"))];
}

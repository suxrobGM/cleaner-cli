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
        yield return new CleanupPath(env.HomePath(".cargo", "registry", "cache"), Description: "registry cache");
        yield return new CleanupPath(env.HomePath(".cargo", "registry", "src"), Description: "extracted sources");
        yield return new CleanupPath(env.HomePath(".cargo", "git"), Description: "git sources");
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
        yield return new CleanupPath(env.HomePath(".rustup", "downloads"));
        yield return new CleanupPath(env.HomePath(".rustup", "tmp"));
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

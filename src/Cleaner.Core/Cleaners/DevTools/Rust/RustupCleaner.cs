using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

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

using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>Nim compiler cache (nimcache); installed nimble packages are kept.</summary>
/// <remarks>
/// Nim's default nimcache is <c>$XDG_CACHE_HOME/nim</c> (<c>~/.cache/nim</c>) on every POSIX system,
/// macOS included, and <c>%USERPROFILE%\nimcache</c> on Windows.
/// </remarks>
public sealed class NimCleaner : DirectoryCleanerBase
{
    public override string Id => "nim";

    public override string Name => "Nim compiler cache";

    public override string Category => Categories.Languages;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        var nimcache = env.IsWindows
            ? env.HomePath("nimcache")
            : Path.Combine(OsPaths.Env(env, "XDG_CACHE_HOME") ?? env.HomePath(".cache"), "nim");

        yield return new CleanupPath(nimcache, DeleteMode.ClearContents);
    }
}

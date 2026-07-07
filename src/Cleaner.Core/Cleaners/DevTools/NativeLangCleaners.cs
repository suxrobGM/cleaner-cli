using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>
/// Conan (C/C++) package cache. <c>conan cache clean "*"</c> removes source/build/download/temp
/// folders but keeps package binaries; with <c>--force</c> the cached packages themselves are
/// removed too (re-downloaded or rebuilt on the next install).
/// </summary>
public sealed class ConanCleaner : ProcessCleanerBase
{
    public override string Id => "conan";

    public override string Name => "Conan cache";

    public override string Category => Categories.Languages;

    protected override string Executable => "conan";

    protected override IReadOnlyList<string> CleanArguments => ["cache", "clean", "*"];

    protected override IEnumerable<IReadOnlyList<string>> CommandSequence(CleanupContext context)
    {
        yield return CleanArguments;
        if (context.Force)
        {
            yield return ["remove", "*", "--confirm"];
        }
    }

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        var conanHome = OsPaths.Env(env, "CONAN_HOME") ?? env.HomePath(".conan2");
        yield return new CleanupPath(Path.Combine(conanHome, "p"), Description: "Conan 2 package cache");
        yield return new CleanupPath(env.HomePath(".conan", "data"), Description: "Conan 1 package cache");
    }
}

/// <summary>Zig global compilation cache.</summary>
public sealed class ZigCleaner : DirectoryCleanerBase
{
    public override string Id => "zig";

    public override string Name => "Zig global cache";

    public override string Category => Categories.Languages;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        if (OsPaths.Env(env, "ZIG_GLOBAL_CACHE_DIR") is { } configured)
        {
            yield return new CleanupPath(configured);
            yield break;
        }

        yield return new CleanupPath(OsPaths.AppCache(env, "zig", "zig", "zig"));

        // zig also uses ~/.cache/zig on macOS in several versions; harmless if absent.
        yield return new CleanupPath(env.HomePath(".cache", "zig"));
    }
}

/// <summary>Swift Package Manager repository/artifact caches (macOS and Linux).</summary>
public sealed class SwiftPmCleaner : DirectoryCleanerBase
{
    public override string Id => "swiftpm";

    public override string Name => "Swift Package Manager cache";

    public override string Category => Categories.Languages;

    public override bool IsApplicable(CleanupContext context) => !context.Environment.IsWindows;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return env.IsMacOs
            ? new CleanupPath(Path.Combine(env.HomeDirectory, "Library", "Caches", "org.swift.swiftpm"))
            : new CleanupPath(Path.Combine(env.CacheDirectory, "org.swift.swiftpm"));
    }
}

/// <summary>opam (OCaml) download cache; keeps switches and installed packages.</summary>
public sealed class OpamCleaner : DirectoryCleanerBase
{
    public override string Id => "opam";

    public override string Name => "opam download cache";

    public override string Category => Categories.Languages;

    public override bool IsApplicable(CleanupContext context) => !context.Environment.IsWindows;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        var root = OsPaths.Env(env, "OPAMROOT") ?? env.HomePath(".opam");
        yield return new CleanupPath(Path.Combine(root, "download-cache"));
    }
}

/// <summary>cpanm (Perl) build work directories (~/.cpanm/work-*).</summary>
public sealed class CpanmCleaner : DirectoryCleanerBase
{
    public override string Id => "cpanm";

    public override string Name => "cpanm work directories";

    public override string Category => Categories.Languages;

    public override bool IsApplicable(CleanupContext context) => !context.Environment.IsWindows;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var root = context.Environment.HomePath(".cpanm");
        foreach (var dir in context.FileSystem.EnumerateDirectories(root))
        {
            if (DirectorySweep.LeafName(dir).StartsWith("work", StringComparison.OrdinalIgnoreCase))
            {
                yield return new CleanupPath(dir, Description: "build workspace");
            }
        }
    }
}

/// <summary>
/// Julia depot caches: precompiled code and logs only. Installed packages, artifacts, and
/// scratchspaces are package-managed state and are deliberately left alone.
/// </summary>
public sealed class JuliaCleaner : DirectoryCleanerBase
{
    public override string Id => "julia";

    public override string Name => "Julia compiled cache";

    public override string Category => Categories.Languages;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;

        // JULIA_DEPOT_PATH is a separator-delimited list; the first entry is the writable depot.
        var depot = OsPaths.Env(env, "JULIA_DEPOT_PATH") is { } configured
            ? configured.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()
            : null;
        depot ??= env.HomePath(".julia");

        yield return new CleanupPath(Path.Combine(depot, "compiled"), Description: "precompiled code");
        yield return new CleanupPath(Path.Combine(depot, "logs"), Description: "logs");
    }
}

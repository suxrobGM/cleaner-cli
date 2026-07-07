using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>Ruby Bundler cache.</summary>
public sealed class GemBundlerCleaner : DirectoryCleanerBase
{
    public override string Id => "bundler";

    public override string Name => "Ruby Bundler cache";

    public override string Category => Categories.Languages;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        [new CleanupPath(context.Environment.HomePath(".bundle", "cache"))];
}

/// <summary>PHP Composer download cache.</summary>
public sealed class ComposerCleaner : DirectoryCleanerBase
{
    public override string Id => "composer";

    public override string Name => "Composer cache";

    public override string Category => Categories.Languages;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return env.IsWindows
            ? new CleanupPath(Path.Combine(env.LocalAppDataDirectory, "Composer"), DeleteMode.ClearContents)
            : new CleanupPath(Path.Combine(env.CacheDirectory, "composer"), DeleteMode.ClearContents);
    }
}

/// <summary>Dart/Flutter pub cache (hosted packages, git checkouts, temp).</summary>
public sealed class PubCleaner : DirectoryCleanerBase
{
    public override string Id => "pub";

    public override string Name => "Dart/Flutter pub cache";

    public override string Category => Categories.Languages;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        var root = OsPaths.Env(env, "PUB_CACHE")
            ?? (env.IsWindows
                ? Path.Combine(env.LocalAppDataDirectory, "Pub", "Cache")
                : env.HomePath(".pub-cache"));

        yield return new CleanupPath(Path.Combine(root, "hosted"), Description: "hosted packages");
        yield return new CleanupPath(Path.Combine(root, "git"), Description: "git packages");
        yield return new CleanupPath(Path.Combine(root, ".tmp"), Description: "temp");
    }
}

/// <summary>Elixir Hex package cache.</summary>
public sealed class HexMixCleaner : DirectoryCleanerBase
{
    public override string Id => "hex";

    public override string Name => "Elixir Hex cache";

    public override string Category => Categories.Languages;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return new CleanupPath(env.HomePath(".hex", "packages"), Description: "Hex packages");
        yield return new CleanupPath(env.HomePath(".mix", "archives"), Description: "Mix archives");
    }
}

/// <summary>vcpkg download and binary-archive caches (C/C++ package manager).</summary>
public sealed class VcpkgCleaner : DirectoryCleanerBase
{
    public override string Id => "vcpkg";

    public override string Name => "vcpkg cache";

    public override string Category => Categories.Languages;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        if (env.IsWindows)
        {
            var root = Path.Combine(env.LocalAppDataDirectory, "vcpkg");
            yield return new CleanupPath(Path.Combine(root, "downloads"), Description: "downloads");
            yield return new CleanupPath(Path.Combine(root, "archives"), Description: "binary cache");
        }
        else
        {
            yield return new CleanupPath(Path.Combine(env.CacheDirectory, "vcpkg", "archives"), Description: "binary cache");
        }
    }
}

/// <summary>Haskell cabal/stack package caches.</summary>
public sealed class CabalStackCleaner : DirectoryCleanerBase
{
    public override string Id => "haskell";

    public override string Name => "Haskell cabal/stack cache";

    public override string Category => Categories.Languages;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return new CleanupPath(env.HomePath(".cabal", "packages"), Description: "cabal packages");
        yield return new CleanupPath(env.HomePath(".stack", "pantry"), Description: "stack pantry");
    }
}

/// <summary>
/// RubyGems maintenance: <c>gem cleanup</c> removes superseded gem versions; the spec index cache is
/// deleted directly. Bundler's cache has its own cleaner.
/// </summary>
public sealed class RubyGemsCleaner : ProcessCleanerBase
{
    public override string Id => "rubygems";

    public override string Name => "RubyGems old versions";

    public override string Category => Categories.Languages;

    protected override string Executable => "gem";

    protected override IReadOnlyList<string> CleanArguments => ["cleanup"];

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        [new CleanupPath(context.Environment.HomePath(".gem", "specs"), DeleteMode.ClearContents, "spec index")];
}

/// <summary>renv (R) global package cache; per-project libraries re-link or reinstall from it.</summary>
public sealed class RenvCleaner : DirectoryCleanerBase
{
    public override string Id => "renv";

    public override string Name => "renv package cache";

    public override string Category => Categories.Languages;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        if (OsPaths.Env(env, "RENV_PATHS_CACHE") is { } configured)
        {
            yield return new CleanupPath(configured, DeleteMode.ClearContents);
            yield break;
        }

        // renv's documented per-OS cache roots.
        if (env.IsWindows)
        {
            yield return new CleanupPath(Path.Combine(env.LocalAppDataDirectory, "renv", "cache"), DeleteMode.ClearContents);
            yield return new CleanupPath(Path.Combine(env.LocalAppDataDirectory, "R", "cache", "R", "renv"), DeleteMode.ClearContents);
        }
        else if (env.IsMacOs)
        {
            yield return new CleanupPath(
                Path.Combine(env.HomeDirectory, "Library", "Caches", "org.R-project.R", "R", "renv"), DeleteMode.ClearContents);
        }
        else
        {
            yield return new CleanupPath(Path.Combine(env.CacheDirectory, "R", "renv"), DeleteMode.ClearContents);
        }
    }
}

/// <summary>LuaRocks download/build cache.</summary>
public sealed class LuaRocksCleaner : DirectoryCleanerBase
{
    public override string Id => "luarocks";

    public override string Name => "LuaRocks cache";

    public override string Category => Categories.Languages;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        [new CleanupPath(OsPaths.AppCache(context.Environment, Path.Combine("LuaRocks", "Cache"), "luarocks", "luarocks"))];
}

/// <summary>Nim compiler cache (nimcache); installed nimble packages are kept.</summary>
public sealed class NimCleaner : DirectoryCleanerBase
{
    public override string Id => "nim";

    public override string Name => "Nim compiler cache";

    public override string Category => Categories.Languages;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        [new CleanupPath(OsPaths.AppCache(context.Environment, "nim", "nim", "nim"), DeleteMode.ClearContents)];
}

/// <summary>TeX Live font/luatex caches under ~/.texlive*/texmf-var (regenerated on use).</summary>
public sealed class TexLiveCleaner : DirectoryCleanerBase
{
    public override string Id => "texlive";

    public override string Name => "TeX Live font caches";

    public override string Category => Categories.Languages;

    public override bool IsApplicable(CleanupContext context) => !context.Environment.IsWindows;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        foreach (var dir in context.FileSystem.EnumerateDirectories(context.Environment.HomeDirectory))
        {
            if (!DirectorySweep.LeafName(dir).StartsWith(".texlive", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            yield return new CleanupPath(Path.Combine(dir, "texmf-var", "luatex-cache"), Description: "luatex cache");
            yield return new CleanupPath(Path.Combine(dir, "texmf-var", "fonts"), Description: "font cache");
        }
    }
}

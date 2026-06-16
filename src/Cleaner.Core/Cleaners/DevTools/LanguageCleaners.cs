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
        var root = env.IsWindows
            ? Path.Combine(env.LocalAppDataDirectory, "Pub", "Cache")
            : env.HomePath(".pub-cache");

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

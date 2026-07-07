using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>Corepack's downloaded package-manager binaries (yarn/pnpm shims re-fetch on demand).</summary>
public sealed class CorepackCleaner : DirectoryCleanerBase
{
    public override string Id => "corepack";

    public override string Name => "Corepack cache";

    public override string Category => Categories.ToolingDownloads;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        if (OsPaths.Env(env, "COREPACK_HOME") is { } configured)
        {
            yield return new CleanupPath(configured, DeleteMode.ClearContents);
            yield break;
        }

        yield return new CleanupPath(env.HomePath(".cache", "node", "corepack"), DeleteMode.ClearContents);
        if (env.IsWindows)
        {
            yield return new CleanupPath(
                Path.Combine(env.LocalAppDataDirectory, "node", "corepack"), DeleteMode.ClearContents);
        }
    }
}

/// <summary>nvm's download cache (installed Node versions are kept).</summary>
public sealed class NvmCleaner : DirectoryCleanerBase
{
    public override string Id => "nvm";

    public override string Name => "nvm download cache";

    public override string Category => Categories.ToolingDownloads;

    // nvm-windows keeps installed runtimes (not a cache) under its root, so this is POSIX-only.
    public override bool IsApplicable(CleanupContext context) => !context.Environment.IsWindows;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        var root = OsPaths.Env(env, "NVM_DIR") ?? env.HomePath(".nvm");
        yield return new CleanupPath(Path.Combine(root, ".cache"), DeleteMode.ClearContents);
    }
}

/// <summary>mise (formerly rtx) download/HTTP cache; installed tools under ~/.local/share/mise are kept.</summary>
public sealed class MiseCleaner : DirectoryCleanerBase
{
    public override string Id => "mise";

    public override string Name => "mise cache";

    public override string Category => Categories.ToolingDownloads;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return new CleanupPath(
            OsPaths.Env(env, "MISE_CACHE_DIR") ?? OsPaths.AppCache(env, Path.Combine("mise", "cache"), "mise", "mise"),
            DeleteMode.ClearContents);
    }
}

/// <summary>asdf download and temp directories; installed tools under installs/ are kept.</summary>
public sealed class AsdfCleaner : DirectoryCleanerBase
{
    public override string Id => "asdf";

    public override string Name => "asdf downloads";

    public override string Category => Categories.ToolingDownloads;

    public override bool IsApplicable(CleanupContext context) => !context.Environment.IsWindows;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        var root = OsPaths.Env(env, "ASDF_DATA_DIR") ?? env.HomePath(".asdf");
        yield return new CleanupPath(Path.Combine(root, "downloads"), DeleteMode.ClearContents, "downloads");
        yield return new CleanupPath(Path.Combine(root, "tmp"), DeleteMode.ClearContents, "temp");
    }
}

/// <summary>SDKMAN! downloaded archives and temp; installed candidates are kept.</summary>
public sealed class SdkmanCleaner : DirectoryCleanerBase
{
    public override string Id => "sdkman";

    public override string Name => "SDKMAN! archives";

    public override string Category => Categories.ToolingDownloads;

    public override bool IsApplicable(CleanupContext context) => !context.Environment.IsWindows;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        var root = OsPaths.Env(env, "SDKMAN_DIR") ?? env.HomePath(".sdkman");
        yield return new CleanupPath(Path.Combine(root, "archives"), DeleteMode.ClearContents, "archives");
        yield return new CleanupPath(Path.Combine(root, "tmp"), DeleteMode.ClearContents, "temp");
    }
}

/// <summary>node-gyp's downloaded Node headers and import libraries (re-fetched per version).</summary>
public sealed class NodeGypCleaner : DirectoryCleanerBase
{
    public override string Id => "node-gyp";

    public override string Name => "node-gyp header cache";

    public override string Category => Categories.ToolingDownloads;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return new CleanupPath(OsPaths.AppCache(env, Path.Combine("node-gyp", "Cache"), "node-gyp", "node-gyp"));

        // Older node-gyp versions used ~/.node-gyp on every OS.
        yield return new CleanupPath(env.HomePath(".node-gyp"));
    }
}

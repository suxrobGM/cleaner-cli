using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>npm package cache. Prefers <c>npm cache clean --force</c>.</summary>
public sealed class NpmCleaner : ProcessCleanerBase
{
    public override string Id => "npm";

    public override string Name => "npm cache";

    public override string Category => Categories.JavaScript;

    protected override string Executable => "npm";

    protected override IReadOnlyList<string> CleanArguments => ["cache", "clean", "--force"];

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return new CleanupPath(Path.Combine(NpmCacheRoot(env), "_cacache"));
    }

    /// <summary>The npm cache root, honoring the npm_config_cache override (either casing).</summary>
    internal static string NpmCacheRoot(Services.IEnvironmentService env) =>
        OsPaths.Env(env, "npm_config_cache", "NPM_CONFIG_CACHE")
        ?? (env.IsWindows
            ? Path.Combine(env.LocalAppDataDirectory, "npm-cache")
            : env.HomePath(".npm"));
}

/// <summary>npx execution cache (npm's <c>_npx</c> directory).</summary>
public sealed class NpxCleaner : DirectoryCleanerBase
{
    public override string Id => "npx";

    public override string Name => "npx cache";

    public override string Category => Categories.JavaScript;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) =>
        [new CleanupPath(Path.Combine(NpmCleaner.NpmCacheRoot(context.Environment), "_npx"))];
}

/// <summary>Yarn cache. Prefers <c>yarn cache clean</c>.</summary>
public sealed class YarnCleaner : ProcessCleanerBase
{
    public override string Id => "yarn";

    public override string Name => "Yarn cache";

    public override string Category => Categories.JavaScript;

    protected override string Executable => "yarn";

    protected override IReadOnlyList<string> CleanArguments => ["cache", "clean"];

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        if (OsPaths.Env(env, "YARN_CACHE_FOLDER") is { } configured)
        {
            yield return new CleanupPath(configured);
            yield break;
        }

        if (env.IsWindows)
        {
            yield return new CleanupPath(Path.Combine(env.LocalAppDataDirectory, "Yarn", "Cache"));
        }
        else
        {
            yield return new CleanupPath(Path.Combine(env.CacheDirectory, "yarn"));
            if (env.IsMacOs)
            {
                // Yarn Classic uses a capital Y; matters on case-sensitive APFS volumes.
                yield return new CleanupPath(Path.Combine(env.HomeDirectory, "Library", "Caches", "Yarn"));
            }
        }

        // Yarn Berry keeps its global mirror separately from the Classic cache.
        yield return new CleanupPath(env.HomePath(".yarn", "berry", "cache"), Description: "Berry global cache");
    }
}

/// <summary>pnpm content-addressable store. Prefers <c>pnpm store prune</c>.</summary>
public sealed class PnpmCleaner : ProcessCleanerBase
{
    public override string Id => "pnpm";

    public override string Name => "pnpm store";

    public override string Category => Categories.JavaScript;

    protected override string Executable => "pnpm";

    protected override IReadOnlyList<string> CleanArguments => ["store", "prune"];

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return env.IsWindows
            ? new CleanupPath(Path.Combine(env.LocalAppDataDirectory, "pnpm", "store"))
            : new CleanupPath(Path.Combine(env.HomeDirectory, ".local", "share", "pnpm", "store"));
    }
}

/// <summary>Bun install cache (covers bunx).</summary>
public sealed class BunCleaner : DirectoryCleanerBase
{
    public override string Id => "bun";

    public override string Name => "Bun cache";

    public override string Category => Categories.JavaScript;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        var cache = OsPaths.Env(env, "BUN_INSTALL_CACHE_DIR");
        if (cache is null)
        {
            var install = OsPaths.Env(env, "BUN_INSTALL") ?? env.HomePath(".bun");
            cache = Path.Combine(install, "install", "cache");
        }

        yield return new CleanupPath(cache);
    }
}

/// <summary>Deno module and dependency cache.</summary>
public sealed class DenoCleaner : DirectoryCleanerBase
{
    public override string Id => "deno";

    public override string Name => "Deno cache";

    public override string Category => Categories.JavaScript;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        var denoDir = env.GetEnvironmentVariable("DENO_DIR");
        if (!string.IsNullOrWhiteSpace(denoDir))
        {
            yield return new CleanupPath(denoDir, DeleteMode.ClearContents);
            yield break;
        }

        yield return new CleanupPath(OsPaths.AppCache(env, "deno", "deno", "deno"), DeleteMode.ClearContents);
    }
}

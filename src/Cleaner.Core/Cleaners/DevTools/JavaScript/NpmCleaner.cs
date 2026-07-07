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

using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>pipx caches and logs only — installed venvs under ~/.local/pipx/venvs are never touched.</summary>
public sealed class PipxCleaner : DirectoryCleanerBase
{
    public override string Id => "pipx";

    public override string Name => "pipx cache & logs";

    public override string Category => Categories.Python;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return new CleanupPath(OsPaths.AppCache(env, Path.Combine("pipx", "Cache"), "pipx", "pipx"), Description: "cache");
        yield return new CleanupPath(env.HomePath(".local", "pipx", ".cache"), Description: "legacy cache");
        yield return new CleanupPath(env.HomePath(".local", "state", "pipx", "log"), Description: "logs");

        if (env.IsWindows)
        {
            yield return new CleanupPath(Path.Combine(env.LocalAppDataDirectory, "pipx", "Logs"), Description: "logs");
        }
    }
}

using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>Neovim cache directory (treesitter/luac artifacts, logs); shada and sessions are kept.</summary>
public sealed class NeovimCleaner : DirectoryCleanerBase
{
    public override string Id => "neovim";

    public override string Name => "Neovim cache";

    public override string Category => Categories.Ides;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        // Neovim's stdpath("cache"): ~/.cache/nvim on POSIX (incl. macOS), %LOCALAPPDATA%\Temp\nvim on Windows.
        yield return env.IsWindows
            ? new CleanupPath(Path.Combine(env.LocalAppDataDirectory, "Temp", "nvim"), DeleteMode.ClearContents)
            : new CleanupPath(env.HomePath(".cache", "nvim"), DeleteMode.ClearContents);
    }
}

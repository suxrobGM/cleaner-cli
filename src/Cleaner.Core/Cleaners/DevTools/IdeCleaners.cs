using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>JetBrains IDE caches and logs (IntelliJ, Rider, PyCharm, ...).</summary>
public sealed class JetBrainsCleaner : DirectoryCleanerBase
{
    /// <summary>Derived-data subdirectories of a per-product dir; everything else may be state.</summary>
    private static readonly string[] ProductCacheSubdirectories = ["caches", "index", "log", "tmp"];

    public override string Id => "jetbrains";

    public override string Name => "JetBrains IDE caches";

    public override string Category => Categories.Ides;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        if (!env.IsWindows)
        {
            // ~/Library/Caches/JetBrains and ~/.cache/JetBrains hold only derived data.
            yield return new CleanupPath(OsPaths.AppCache(env, "JetBrains", "JetBrains", "JetBrains"), DeleteMode.ClearContents);
            yield break;
        }

        // On Windows %LOCALAPPDATA%\JetBrains also contains Toolbox (installed IDE binaries) and
        // per-product state such as LocalHistory, so only the cache subdirs of each product dir go.
        var root = Path.Combine(env.LocalAppDataDirectory, "JetBrains");
        foreach (var productDir in context.FileSystem.EnumerateDirectories(root))
        {
            if (DirectorySweep.LeafName(productDir).Equals("Toolbox", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var sub in ProductCacheSubdirectories)
            {
                yield return new CleanupPath(
                    Path.Combine(productDir, sub),
                    Description: $"{DirectorySweep.LeafName(productDir)} {sub}");
            }
        }
    }
}

/// <summary>
/// VS Code cache directories (keeps settings and installed extensions). Also covers forks with the
/// same layout: Cursor, VSCodium, and Windsurf.
/// </summary>
public sealed class VsCodeCleaner : DirectoryCleanerBase
{
    private static readonly string[] CacheSubdirectories =
        ["Cache", "CachedData", "Code Cache", "GPUCache", "logs", "CachedExtensionVSIXs"];

    /// <summary>App-data folder names of VS Code and its forks.</summary>
    private static readonly string[] AppFolders = ["Code", "Cursor", "VSCodium", "Windsurf"];

    public override string Id => "vscode";

    public override string Name => "VS Code / Cursor / VSCodium caches";

    public override string Category => Categories.Ides;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        foreach (var app in AppFolders)
        {
            var userRoot = env.IsWindows
                ? Path.Combine(env.AppDataDirectory, app)
                : env.IsMacOs
                    ? Path.Combine(env.HomeDirectory, "Library", "Application Support", app)
                    : Path.Combine(env.HomeDirectory, ".config", app);

            foreach (var sub in CacheSubdirectories)
            {
                yield return new CleanupPath(Path.Combine(userRoot, sub), Description: $"{app} {sub}");
            }
        }
    }
}

/// <summary>Visual Studio caches: project-local <c>.vs</c> and the ComponentModelCache (Windows).</summary>
public sealed class VisualStudioCleaner : DirectoryCleanerBase
{
    public override string Id => "visualstudio";

    public override string Name => "Visual Studio caches";

    public override string Category => Categories.Ides;

    public override bool IsApplicable(CleanupContext context) => context.Environment.IsWindows;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return new CleanupPath(Path.Combine(context.WorkingDirectory, ".vs"), Description: "project .vs");

        var vsRoot = Path.Combine(env.LocalAppDataDirectory, "Microsoft", "VisualStudio");
        foreach (var versionDir in context.FileSystem.EnumerateDirectories(vsRoot))
        {
            yield return new CleanupPath(Path.Combine(versionDir, "ComponentModelCache"), Description: "ComponentModelCache");
        }
    }
}

/// <summary>
/// Xcode derived data, device-support symbol caches, and simulator caches (macOS). All are
/// regenerated on the next build or device connect; Archives (user data) are never touched.
/// </summary>
public sealed class XcodeCleaner : DirectoryCleanerBase
{
    public override string Id => "xcode";

    public override string Name => "Xcode caches";

    public override string Category => Categories.Ides;

    public override bool IsApplicable(CleanupContext context) => context.Environment.IsMacOs;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var developer = Path.Combine(context.Environment.HomeDirectory, "Library", "Developer");
        yield return new CleanupPath(
            Path.Combine(developer, "Xcode", "DerivedData"), DeleteMode.ClearContents, "DerivedData");
        yield return new CleanupPath(
            Path.Combine(developer, "Xcode", "iOS DeviceSupport"), DeleteMode.ClearContents, "iOS device support");
        yield return new CleanupPath(
            Path.Combine(developer, "Xcode", "watchOS DeviceSupport"), DeleteMode.ClearContents, "watchOS device support");
        yield return new CleanupPath(
            Path.Combine(developer, "CoreSimulator", "Caches"), DeleteMode.ClearContents, "simulator caches");
    }
}

/// <summary>Zed editor cache.</summary>
public sealed class ZedCleaner : DirectoryCleanerBase
{
    public override string Id => "zed";

    public override string Name => "Zed cache";

    public override string Category => Categories.Ides;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return env.IsMacOs
            ? new CleanupPath(Path.Combine(env.HomeDirectory, "Library", "Caches", "dev.zed.Zed"), DeleteMode.ClearContents)
            : new CleanupPath(OsPaths.AppCache(env, Path.Combine("Zed", "cache"), "Zed", "zed"), DeleteMode.ClearContents);
    }
}

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

using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.DevTools;

/// <summary>JetBrains IDE caches and logs (IntelliJ, Rider, PyCharm, ...).</summary>
public sealed class JetBrainsCleaner : DirectoryCleanerBase
{
    public override string Id => "jetbrains";

    public override string Name => "JetBrains IDE caches";

    public override string Category => Categories.Ides;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return new CleanupPath(OsPaths.AppCache(env, "JetBrains", "JetBrains", "JetBrains"), DeleteMode.ClearContents);
    }
}

/// <summary>VS Code cache directories (keeps settings and installed extensions).</summary>
public sealed class VsCodeCleaner : DirectoryCleanerBase
{
    private static readonly string[] CacheSubdirectories =
        ["Cache", "CachedData", "Code Cache", "GPUCache", "logs", "CachedExtensionVSIXs"];

    public override string Id => "vscode";

    public override string Name => "VS Code caches";

    public override string Category => Categories.Ides;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        var userRoot = env.IsWindows
            ? Path.Combine(env.AppDataDirectory, "Code")
            : env.IsMacOs
                ? Path.Combine(env.HomeDirectory, "Library", "Application Support", "Code")
                : Path.Combine(env.HomeDirectory, ".config", "Code");

        foreach (var sub in CacheSubdirectories)
        {
            yield return new CleanupPath(Path.Combine(userRoot, sub), Description: sub);
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

/// <summary>Xcode DerivedData build products (macOS).</summary>
public sealed class XcodeCleaner : DirectoryCleanerBase
{
    public override string Id => "xcode";

    public override string Name => "Xcode DerivedData";

    public override string Category => Categories.Ides;

    public override bool IsApplicable(CleanupContext context) => context.Environment.IsMacOs;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var env = context.Environment;
        yield return new CleanupPath(
            Path.Combine(env.HomeDirectory, "Library", "Developer", "Xcode", "DerivedData"),
            DeleteMode.ClearContents,
            "DerivedData");
    }
}

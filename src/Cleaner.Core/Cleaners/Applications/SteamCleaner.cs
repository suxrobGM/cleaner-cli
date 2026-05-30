using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners;
using Cleaner.Core.Cleaners.Base;
using Cleaner.Core.Services;

namespace Cleaner.Core.Cleaners.Applications;

/// <summary>
/// Clears Steam's safe caches — shader, download, web/HTML caches, logs and dumps. Never touches
/// installed games (<c>steamapps/common</c>) or user/save data.
/// </summary>
public sealed class SteamCleaner : DirectoryCleanerBase
{
    public override string Id => "steam";

    public override string Name => "Steam caches";

    public override string Category => Categories.Applications;

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context)
    {
        var root = ResolveSteamRoot(context);
        if (root is null)
        {
            yield break;
        }

        yield return new CleanupPath(Path.Combine(root, "appcache", "httpcache"), DeleteMode.ClearContents, "http cache");
        yield return new CleanupPath(Path.Combine(root, "config", "htmlcache"), DeleteMode.ClearContents, "html cache");
        yield return new CleanupPath(Path.Combine(root, "steamapps", "shadercache"), DeleteMode.ClearContents, "shader cache");
        yield return new CleanupPath(Path.Combine(root, "steamapps", "downloading"), DeleteMode.ClearContents, "download cache");
        yield return new CleanupPath(Path.Combine(root, "steamapps", "temp"), DeleteMode.ClearContents, "download temp");
        yield return new CleanupPath(Path.Combine(root, "logs"), DeleteMode.ClearContents, "logs");
        yield return new CleanupPath(Path.Combine(root, "dumps"), DeleteMode.ClearContents, "dumps");
    }

    private static string? ResolveSteamRoot(CleanupContext context)
    {
        var env = context.Environment;

        foreach (var candidate in CandidateRoots(env))
        {
            if (!string.IsNullOrWhiteSpace(candidate) && context.FileSystem.DirectoryExists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static IEnumerable<string> CandidateRoots(IEnvironmentService env)
    {
        if (env.IsWindows)
        {
            yield return Path.Combine(env.GetEnvironmentVariable("ProgramFiles(x86)") ?? @"C:\Program Files (x86)", "Steam");
            yield return Path.Combine(env.GetEnvironmentVariable("ProgramFiles") ?? @"C:\Program Files", "Steam");
        }
        else if (env.IsMacOs)
        {
            yield return Path.Combine(env.HomeDirectory, "Library", "Application Support", "Steam");
        }
        else
        {
            yield return Path.Combine(env.HomeDirectory, ".steam", "steam");
            yield return Path.Combine(env.HomeDirectory, ".local", "share", "Steam");
        }
    }
}

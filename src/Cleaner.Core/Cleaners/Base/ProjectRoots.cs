using Cleaner.Core.Abstractions;

namespace Cleaner.Core.Cleaners.Base;

/// <summary>
/// Decides which folders a project search may start from. The profile, drive roots, and system folders
/// would reach installed tools, so they are refused with a note on how to point Cleaner at a workspace.
/// </summary>
internal static class ProjectRoots
{
    /// <summary>The <see cref="CleanupContext.ScanRoots"/> the guard allows.</summary>
    public static IReadOnlyList<string> Allowed(CleanupContext context) =>
        [.. context.ScanRoots.Where(root => context.Guard.WhyNotProjectRoot(root) is null)];

    /// <summary>Whether a single project folder (usually the working directory) may be searched.</summary>
    public static bool IsAllowed(CleanupContext context, string root) =>
        context.Guard.WhyNotProjectRoot(root) is null;

    /// <summary>One note per refused root, naming the fix.</summary>
    public static IEnumerable<string> Refusals(CleanupContext context)
    {
        foreach (var root in context.ScanRoots)
        {
            if (context.Guard.WhyNotProjectRoot(root) is { } why)
            {
                yield return $"did not scan {root}: it {why}. Start Cleaner in a project folder or pass --path <workspace>.";
            }
        }
    }
}

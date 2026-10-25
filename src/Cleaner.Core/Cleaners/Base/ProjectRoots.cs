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

    /// <summary>
    /// A path inside the working directory, or null when the guard refuses the working directory as a
    /// project (the profile, a drive root, a system folder).
    /// </summary>
    public static string? InWorkingDirectory(CleanupContext context, params string[] parts) =>
        context.Guard.WhyNotProjectRoot(context.WorkingDirectory) is null
            ? Path.Combine([context.WorkingDirectory, .. parts])
            : null;

    /// <summary>
    /// Matching folders below the allowed scan roots. The search enters only folders the guard allows
    /// and that <paramref name="canEnter"/> (when given) also allows.
    /// </summary>
    public static IEnumerable<string> Search(
        CleanupContext context,
        Func<string, bool> isMatch,
        Func<string, bool>? canEnter = null) =>
        DirectorySearch.Find(
            context.FileSystem,
            Allowed(context),
            isMatch,
            dir => context.Guard.CanEnter(dir) && (canEnter is null || canEnter(dir)));

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

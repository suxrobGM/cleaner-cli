using Cleaner.Core.Abstractions;

namespace Cleaner.Core.Cleaners.Base;

/// <summary>
/// Decides which folders a project sweep may start from. Started from the profile, a drive root, or
/// a system folder, a sweep would reach installed tools, so those roots are refused with a note
/// that says how to point Cleaner at a workspace instead.
/// </summary>
internal static class ProjectRoots
{
    /// <summary>The scan roots a sweep may walk.</summary>
    public static IReadOnlyList<string> Allowed(CleanupContext context) =>
        [.. context.ScanRoots.Where(root => context.Guard.WhyNotProjectRoot(root) is null)];

    /// <summary>Whether a single project folder (usually the working directory) may be swept.</summary>
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

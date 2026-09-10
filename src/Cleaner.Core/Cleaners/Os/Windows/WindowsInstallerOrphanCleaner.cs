using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.Os;

/// <summary>
/// Cached <c>.msi</c>/<c>.msp</c> packages in <c>C:\Windows\Installer</c> that no installed
/// product or patch references.
/// </summary>
/// <remarks>
/// The live set comes from <c>LocalPackage</c> values under the Installer's <c>UserData</c> registry
/// key. A failed or empty query is treated as unknown; deleting in that case could break repair.
/// </remarks>
public sealed class WindowsInstallerOrphanCleaner : WindowsCleanerBase
{
    private const string UserDataKey = @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData";

    /// <summary>
    /// Cached orphan results for the current run context.
    /// </summary>
    private (CleanupContext Context, IReadOnlyList<CleanupPath> Targets)? _orphans;

    public override string Id => "windows-installer-orphans";

    public override string Name => "Orphaned Windows Installer packages";

    public override bool RequiresElevation => true;

    public override string ConfirmationWarning =>
        "if a package is still needed by a product this can't see, that product loses repair, " +
        "patch, and uninstall — a fresh Windows update run beforehand keeps the registry accurate";

    /// <summary>Relevant wherever the package cache exists; whether any package is orphaned costs a scan.</summary>
    public override bool IsAvailable(CleanupContext context) => CachedPackages(context).Any();

    protected override async ValueTask<IEnumerable<CleanupPath>> GetTargetsAsync(
        CleanupContext context,
        CancellationToken cancellationToken)
    {
        if (_orphans is { } cached && ReferenceEquals(cached.Context, context))
        {
            return cached.Targets;
        }

        var targets = await OrphansAsync(context, cancellationToken).ConfigureAwait(false);
        _orphans = (context, targets);
        return targets;
    }

    /// <summary>Cached packages that no installed product or patch still references.</summary>
    private static async Task<IReadOnlyList<CleanupPath>> OrphansAsync(
        CleanupContext context,
        CancellationToken cancellationToken)
    {
        var cached = CachedPackages(context).ToList();
        if (cached.Count == 0)
        {
            return [];
        }

        var referenced = await ReferencedPackagesAsync(context, cancellationToken).ConfigureAwait(false);

        // No answer from the registry means "unknown", never "nothing is referenced".
        return referenced.Count == 0
            ? []
            : [.. cached
                .Where(path => !referenced.Contains(Normalize(path)))
                .Select(path => new CleanupPath(path, DeleteMode.DeleteFile, "orphaned package"))];
    }

    private static IEnumerable<string> CachedPackages(CleanupContext context)
    {
        var windows = context.Environment.WindowsDirectory;
        if (windows is null)
        {
            return [];
        }

        var installer = Path.Combine(windows, "Installer");
        return context.FileSystem.DirectoryExists(installer)
            ? context.FileSystem.EnumerateFiles(installer, "*.ms*")
                .Where(path => IsPackage(Path.GetExtension(path)))
            : [];
    }

    private static bool IsPackage(string extension) =>
        extension.Equals(".msi", StringComparison.OrdinalIgnoreCase)
        || extension.Equals(".msp", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Normalizes separators so registry and filesystem paths compare consistently.
    /// </summary>
    private static string Normalize(string path) =>
        path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

    /// <summary>
    /// Reads every <c>LocalPackage</c> path under the Installer's <c>UserData</c> key.
    /// </summary>
    private static async Task<HashSet<string>> ReferencedPackagesAsync(CleanupContext context, CancellationToken cancellationToken)
    {
        var referenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = await context.ProcessRunner
            .RunAsync("reg", ["query", UserDataKey, "/s", "/v", "LocalPackage"], cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (!result.Success)
        {
            return referenced;
        }

        foreach (var line in result.StandardOutput.Split('\n'))
        {
            var marker = line.IndexOf("REG_SZ", StringComparison.OrdinalIgnoreCase);
            if (marker < 0)
            {
                continue;
            }

            var value = line[(marker + "REG_SZ".Length)..].Trim();
            if (value.Length > 0)
            {
                referenced.Add(Normalize(value));
            }
        }

        return referenced;
    }
}

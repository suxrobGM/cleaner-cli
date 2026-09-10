using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners.Base;

namespace Cleaner.Core.Cleaners.Os;

/// <summary>
/// Cached <c>.msi</c>/<c>.msp</c> packages in <c>C:\Windows\Installer</c> that no installed
/// product or patch still references. Windows caches one per product for repair and uninstall but
/// never prunes them, so after a few Visual Studio or Office upgrades the strays dominate.
/// </summary>
/// <remarks>
/// The live set comes from the <c>LocalPackage</c> values under the Installer's <c>UserData</c>
/// registry key, read in a single <c>reg query</c> so no registry API is needed. If that read fails
/// or comes back empty the cleaner does nothing: treating an empty set as "nothing is referenced"
/// would wipe the cache and break repair for everything installed.
/// </remarks>
public sealed class WindowsInstallerOrphanCleaner : WindowsCleanerBase
{
    private const string UserDataKey = @"HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Installer\UserData";

    public override string Id => "windows-installer-orphans";

    public override string Name => "Orphaned Windows Installer packages";

    public override bool RequiresElevation => true;

    public override string ConfirmationWarning =>
        "if a package is still needed by a product this can't see, that product loses repair, " +
        "patch, and uninstall — a fresh Windows update run beforehand keeps the registry accurate";

    public override bool IsAvailable(CleanupContext context) => CachedPackages(context).Any();

    public override async Task<ScanResult> ScanAsync(CleanupContext context, CancellationToken cancellationToken = default)
    {
        var orphans = await OrphansAsync(context, cancellationToken).ConfigureAwait(false);
        return new ScanResult(
            [.. orphans.Select(path => new CleanupTarget(path, context.FileSystem.GetFileSize(path), "orphaned package"))]);
    }

    public override async Task<CleanResult> CleanAsync(
        CleanupContext context,
        IProgress<CleanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var orphans = await OrphansAsync(context, cancellationToken).ConfigureAwait(false);
        long freed = 0;
        var removed = 0;
        var errors = new List<string>();

        foreach (var path in orphans)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var size = context.FileSystem.GetFileSize(path);

            try
            {
                if (!context.DryRun)
                {
                    context.FileSystem.DeleteFile(path);
                }

                freed += size;
                removed++;
                progress?.Report(new CleanProgress(path, size));
            }
            catch (Exception ex)
            {
                errors.Add($"{path}: {ex.Message}");
            }
        }

        return new CleanResult(freed, removed, errors);
    }

    protected override IEnumerable<CleanupPath> GetTargets(CleanupContext context) => [];

    /// <summary>Cached packages that no installed product or patch still references.</summary>
    private static async Task<IReadOnlyList<string>> OrphansAsync(CleanupContext context, CancellationToken cancellationToken)
    {
        var cached = CachedPackages(context).ToList();
        if (cached.Count == 0)
        {
            return [];
        }

        var referenced = await ReferencedPackagesAsync(context, cancellationToken).ConfigureAwait(false);

        // No answer from the registry means "unknown", never "nothing is referenced".
        return referenced.Count == 0 ? [] : [.. cached.Where(path => !referenced.Contains(path))];
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
    /// Every <c>LocalPackage</c> path recorded under the Installer's <c>UserData</c> key. Compared
    /// case-insensitively because the registry stores them with a different casing (<c>C:\WINDOWS</c>)
    /// than enumeration returns.
    /// </summary>
    private static async Task<HashSet<string>> ReferencedPackagesAsync(CleanupContext context, CancellationToken cancellationToken)
    {
        var referenced = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = await context.ProcessRunner
            .RunAsync("reg", ["query", UserDataKey, "/s", "/v", "LocalPackage"], cancellationToken)
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
                referenced.Add(value);
            }
        }

        return referenced;
    }
}

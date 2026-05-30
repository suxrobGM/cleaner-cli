using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using Cleaner.Core.Utils;

namespace Cleaner.Core.Services;

/// <inheritdoc cref="IUpdateService"/>
public sealed class UpdateService(IGitHubReleaseClient releaseClient, IEnvironmentService environment) : IUpdateService
{
    private const string BackupSuffix = ".old";

    private string? cachedVersion;

    public string CurrentVersion => cachedVersion ??= ReadCurrentVersion();

    public async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var release = await releaseClient.GetLatestReleaseAsync(cancellationToken).ConfigureAwait(false);
        if (release is null || string.IsNullOrWhiteSpace(release.TagName))
        {
            return new UpdateCheckResult(CurrentVersion, null, false, null, null);
        }

        var latest = VersionUtils.Normalize(release.TagName);
        var isNewer = VersionUtils.IsNewer(latest, CurrentVersion);
        var asset = VersionUtils.SelectAsset(release.Assets, RuntimeInformation.RuntimeIdentifier);

        return new UpdateCheckResult(CurrentVersion, latest, isNewer, release.HtmlUrl, asset);
    }

    public async Task ApplyAsync(
        UpdateCheckResult check,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (check.Asset is null)
        {
            throw new InvalidOperationException(
                $"No release asset is available for this platform ({RuntimeInformation.RuntimeIdentifier}). " +
                "Download the update manually from the releases page.");
        }

        var currentExe = Environment.ProcessPath
            ?? throw new InvalidOperationException("Could not determine the running executable's path.");

        var workDir = Path.Combine(environment.TempDirectory, "cleaner-update-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);
        try
        {
            var archivePath = Path.Combine(workDir, check.Asset.Name);
            await releaseClient
                .DownloadAssetAsync(check.Asset.DownloadUrl, archivePath, progress, cancellationToken)
                .ConfigureAwait(false);

            var extractDir = Path.Combine(workDir, "extracted");
            Directory.CreateDirectory(extractDir);
            Extract(archivePath, extractDir);

            var newBinary = FindBinary(extractDir)
                ?? throw new InvalidOperationException("The downloaded archive did not contain a 'cleaner' binary.");

            SwapBinary(newBinary, currentExe);
            Relaunch(currentExe);
        }
        finally
        {
            TryDeleteDirectory(workDir);
        }
    }

    public void CleanupStaleBackup()
    {
        var currentExe = Environment.ProcessPath;
        if (string.IsNullOrEmpty(currentExe))
        {
            return;
        }

        var backup = currentExe + BackupSuffix;
        try
        {
            if (File.Exists(backup))
            {
                File.Delete(backup);
            }
        }
        catch
        {
            // The previous binary may still be locked briefly; we'll get it next run.
        }
    }

    private static string ReadCurrentVersion()
    {
        var informational = Assembly.GetEntryAssembly()
            ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        var version = VersionUtils.Normalize(informational ?? string.Empty);
        return string.IsNullOrEmpty(version) ? "0.0.0-dev" : version;
    }

    private void Extract(string archivePath, string destination)
    {
        if (archivePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            ZipFile.ExtractToDirectory(archivePath, destination, overwriteFiles: true);
            return;
        }

        // .tar.gz — gunzip the stream straight into the tar extractor.
        using var fileStream = File.OpenRead(archivePath);
        using var gzip = new GZipStream(fileStream, CompressionMode.Decompress);
        TarFile.ExtractToDirectory(gzip, destination, overwriteFiles: true);
    }

    private string? FindBinary(string directory)
    {
        var name = environment.IsWindows ? "cleaner.exe" : "cleaner";
        return Directory
            .EnumerateFiles(directory, name, SearchOption.AllDirectories)
            .FirstOrDefault();
    }

    private void SwapBinary(string newBinary, string currentExe)
    {
        try
        {
            // Use the framework OS check (not IEnvironmentService) so the platform-compatibility
            // analyzer can flow-analyze the Unix-only File.SetUnixFileMode call below.
            if (OperatingSystem.IsWindows())
            {
                // A running .exe can't be deleted, but it can be renamed out of the way.
                var backup = currentExe + BackupSuffix;
                if (File.Exists(backup))
                {
                    File.Delete(backup);
                }

                File.Move(currentExe, backup);
                File.Move(newBinary, currentExe);
            }
            else
            {
                // On Unix the running process keeps the old inode; overwriting the path is safe.
                File.Move(newBinary, currentExe, overwrite: true);
                File.SetUnixFileMode(
                    currentExe,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                    UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                    UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
            }
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            var hint = environment.IsWindows ? "an administrator" : "root (sudo)";
            throw new InvalidOperationException(
                $"Could not replace the binary at '{currentExe}'. Re-run the update as {hint}, " +
                "or move 'cleaner' to a writable location.", ex);
        }
    }

    private static void Relaunch(string currentExe)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = currentExe,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("--version");
        Process.Start(startInfo);
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // Temp dir cleanup is best-effort.
        }
    }
}

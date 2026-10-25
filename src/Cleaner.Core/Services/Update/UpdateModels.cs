namespace Cleaner.Core.Services;

/// <summary>The release asset chosen for the current platform.</summary>
public sealed record UpdateAsset(string Name, string DownloadUrl);

public sealed record UpdateCheckResult(
    string CurrentVersion,
    string? LatestVersion,
    bool IsUpdateAvailable,
    string? ReleaseUrl,
    UpdateAsset? Asset);

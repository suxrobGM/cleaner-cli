using System.Text.Json.Serialization;

namespace Cleaner.Core.Services;

public sealed class GitHubAsset
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("browser_download_url")]
    public string BrowserDownloadUrl { get; set; } = string.Empty;
}

/// <summary>The GitHub REST API release fields Cleaner uses.</summary>
public sealed class GitHubRelease
{
    [JsonPropertyName("tag_name")]
    public string TagName { get; set; } = string.Empty;

    [JsonPropertyName("html_url")]
    public string HtmlUrl { get; set; } = string.Empty;

    [JsonPropertyName("prerelease")]
    public bool Prerelease { get; set; }

    [JsonPropertyName("draft")]
    public bool Draft { get; set; }

    [JsonPropertyName("assets")]
    public List<GitHubAsset> Assets { get; set; } = [];
}

/// <summary>Source-generated so release deserialization stays Native-AOT safe.</summary>
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(GitHubRelease))]
public sealed partial class GitHubJsonContext : JsonSerializerContext;

public interface IGitHubReleaseClient
{
    /// <summary>The latest published (non-draft, non-prerelease) release, or null if none or on error.</summary>
    Task<GitHubRelease?> GetLatestReleaseAsync(CancellationToken cancellationToken = default);

    /// <summary>Reports 0.0–1.0 progress when the length is known.</summary>
    Task DownloadAssetAsync(
        string downloadUrl,
        string destinationPath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}

using System.Text.Json.Serialization;

namespace Cleaner.Core.Services;

/// <summary>A single downloadable file attached to a GitHub release.</summary>
public sealed class GitHubAsset
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("browser_download_url")]
    public string BrowserDownloadUrl { get; set; } = string.Empty;
}

/// <summary>A GitHub release as returned by the REST API (only the fields we use).</summary>
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

/// <summary>Source-generated JSON metadata for Native-AOT-safe release deserialization.</summary>
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(GitHubRelease))]
public sealed partial class GitHubJsonContext : JsonSerializerContext;

/// <summary>GitHub API boundary for release networking and JSON.</summary>
public interface IGitHubReleaseClient
{
    /// <summary>The latest published (non-draft, non-prerelease) release, or null if none / on error.</summary>
    Task<GitHubRelease?> GetLatestReleaseAsync(CancellationToken cancellationToken = default);

    /// <summary>Downloads an asset and reports 0.0–1.0 progress when length is known.</summary>
    Task DownloadAssetAsync(
        string downloadUrl,
        string destinationPath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}

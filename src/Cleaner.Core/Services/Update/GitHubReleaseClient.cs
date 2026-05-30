using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace Cleaner.Core.Services;

/// <inheritdoc cref="IGitHubReleaseClient"/>
public sealed class GitHubReleaseClient : IGitHubReleaseClient, IDisposable
{
    // The repository whose releases drive auto-update. Must match the real remote.
    private const string Owner = "suxrobGM";
    private const string Repo = "cleaner-cli";

    private const string ProductName = "cleaner-updater";

    private readonly HttpClient http;

    public GitHubReleaseClient()
        : this(new HttpClient())
    {
    }

    /// <summary>Test/seam constructor — lets callers inject a pre-configured <see cref="HttpClient"/>.</summary>
    public GitHubReleaseClient(HttpClient httpClient)
    {
        http = httpClient;
        // GitHub rejects requests without a User-Agent and recommends pinning the API version.
        http.DefaultRequestHeaders.UserAgent.ParseAdd($"{ProductName}/1.0");
        http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        http.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
    }

    public async Task<GitHubRelease?> GetLatestReleaseAsync(CancellationToken cancellationToken = default)
    {
        var url = $"https://api.github.com/repos/{Owner}/{Repo}/releases/latest";
        using var response = await http.GetAsync(url, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            // 404 simply means no published release yet; treat any non-success as "nothing to offer".
            return null;
        }

        return await response.Content
            .ReadFromJsonAsync(GitHubJsonContext.Default.GitHubRelease, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task DownloadAssetAsync(
        string downloadUrl,
        string destinationPath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        using var response = await http
            .GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var total = response.Content.Headers.ContentLength;
        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using var destination = File.Create(destinationPath);

        var buffer = new byte[81920];
        long copied = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            copied += read;
            if (total is > 0)
            {
                progress?.Report((double)copied / total.Value);
            }
        }

        progress?.Report(1.0);
    }

    public void Dispose() => http.Dispose();
}

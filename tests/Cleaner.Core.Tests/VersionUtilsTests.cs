using System.Text.Json;
using Cleaner.Core.Services;
using Cleaner.Core.Utils;
using Xunit;

namespace Cleaner.Core.Tests;

public sealed class VersionUtilsTests
{
    [Theory]
    [InlineData("v1.2.3", "1.2.3")]
    [InlineData("1.2.3", "1.2.3")]
    [InlineData("V1.0.0", "1.0.0")]
    [InlineData("1.0.0+abc123", "1.0.0")]
    [InlineData("v1.0.0+build.5", "1.0.0")]
    [InlineData("  v2.0.0  ", "2.0.0")]
    [InlineData("", "")]
    public void Normalize_strips_prefix_and_build_metadata(string input, string expected) =>
        Assert.Equal(expected, VersionUtils.Normalize(input));

    [Theory]
    [InlineData("1.0.0", "1.2.0", -1)]
    [InlineData("1.2.0", "1.0.0", 1)]
    [InlineData("1.10.0", "1.9.0", 1)]
    [InlineData("1.0.0", "1.0.0", 0)]
    [InlineData("v1.0.0", "1.0.0", 0)]
    [InlineData("1.0.0+abc", "1.0.0+def", 0)]
    [InlineData("2.0.0", "1.9.9", 1)]
    [InlineData("1.0.0-rc1", "1.0.0", -1)]
    [InlineData("1.0.0", "1.0.0-rc1", 1)]
    [InlineData("1.0.0-rc1", "1.0.0-rc2", -1)]
    [InlineData("1.0", "1.0.0", 0)]
    public void Compare_orders_semver(string a, string b, int expectedSign) =>
        Assert.Equal(expectedSign, Math.Sign(VersionUtils.Compare(a, b)));

    [Theory]
    [InlineData("1.2.0", "1.0.0", true)]
    [InlineData("1.0.0", "1.0.0", false)]
    [InlineData("0.9.0", "1.0.0", false)]
    public void IsNewer_reports_strict_improvement(string latest, string current, bool expected) =>
        Assert.Equal(expected, VersionUtils.IsNewer(latest, current));

    [Fact]
    public void SelectAsset_matches_runtime_identifier()
    {
        var assets = new[]
        {
            new GitHubAsset { Name = "cleaner-v1.0.0-linux-x64.tar.gz", BrowserDownloadUrl = "https://example/linux" },
            new GitHubAsset { Name = "cleaner-v1.0.0-win-x64.zip", BrowserDownloadUrl = "https://example/win" },
        };

        var selected = VersionUtils.SelectAsset(assets, "win-x64");

        Assert.NotNull(selected);
        Assert.Equal("cleaner-v1.0.0-win-x64.zip", selected!.Name);
        Assert.Equal("https://example/win", selected.DownloadUrl);
    }

    [Fact]
    public void SelectAsset_returns_null_when_platform_absent()
    {
        var assets = new[]
        {
            new GitHubAsset { Name = "cleaner-v1.0.0-linux-x64.tar.gz", BrowserDownloadUrl = "https://example/linux" },
        };

        Assert.Null(VersionUtils.SelectAsset(assets, "osx-arm64"));
    }

    [Fact]
    public void GitHubRelease_deserializes_through_source_generated_context()
    {
        const string json = """
        {
          "tag_name": "v1.4.2",
          "html_url": "https://github.com/suxrobGM/cleaner-cli/releases/tag/v1.4.2",
          "draft": false,
          "prerelease": false,
          "assets": [
            { "name": "cleaner-v1.4.2-win-x64.zip", "browser_download_url": "https://example/win.zip" },
            { "name": "cleaner-v1.4.2-linux-x64.tar.gz", "browser_download_url": "https://example/linux.tar.gz" }
          ]
        }
        """;

        var release = JsonSerializer.Deserialize(json, GitHubJsonContext.Default.GitHubRelease);

        Assert.NotNull(release);
        Assert.Equal("v1.4.2", release!.TagName);
        Assert.Equal("https://github.com/suxrobGM/cleaner-cli/releases/tag/v1.4.2", release.HtmlUrl);
        Assert.False(release.Draft);
        Assert.Equal(2, release.Assets.Count);
        Assert.Equal("cleaner-v1.4.2-win-x64.zip", release.Assets[0].Name);
    }
}

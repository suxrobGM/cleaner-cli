using System.Text.RegularExpressions;
using Cleaner.Cli.Infrastructure;
using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners;
using Cleaner.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cleaner.Core.Tests;

/// <summary>
/// Builds the real composition root and audits the cleaner catalog, so a forgotten registration,
/// duplicate id, or off-convention id fails a test instead of shipping.
/// </summary>
public sealed class CompositionRootTests
{
    private static readonly string[] KnownCategories =
    [
        Categories.PackageManagers, Categories.JavaScript, Categories.Python, Categories.Rust,
        Categories.Go, Categories.Jvm, Categories.MachineLearning, Categories.GameDev,
        Categories.Mobile, Categories.Languages, Categories.BuildCaches, Categories.Containers,
        Categories.Ides, Categories.ToolingDownloads, Categories.ProjectLocal,
        Categories.OperatingSystem, Categories.SystemPackageManagers, Categories.Applications,
    ];

    private static ICleanerRegistry BuildRegistry()
    {
        var services = new ServiceCollection();
        services.AddCleaner();
        using var provider = services.BuildServiceProvider();
        // CleanerRegistry's id dictionary throws on duplicates, so resolving it is itself an assertion.
        return provider.GetRequiredService<ICleanerRegistry>();
    }

    [Fact]
    public void Every_cleaner_id_is_unique_and_kebab_case()
    {
        var registry = BuildRegistry();

        Assert.All(registry.All, c => Assert.Matches(new Regex("^[a-z0-9][a-z0-9-]*$"), c.Id));
        Assert.Equal(registry.All.Count, registry.All.Select(c => c.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void Every_cleaner_uses_a_known_category_and_has_a_name()
    {
        var registry = BuildRegistry();

        Assert.All(registry.All, c => Assert.Contains(c.Category, KnownCategories));
        Assert.All(registry.All, c => Assert.False(string.IsNullOrWhiteSpace(c.Name)));
    }

    [Fact]
    public void Catalog_contains_the_expected_cleaners()
    {
        var registry = BuildRegistry();

        // Spot-check ids across old and new groups; the count catches silently dropped registrations.
        foreach (var id in new[]
                 {
                     "nuget", "npm", "uv", "conan", "zig", "julia", "podman", "helm", "pipx",
                     "corepack", "mise", "winget", "flatpak", "nix", "telegram", "gpu-installers",
                     "game-launchers", "unreal", "winsxs", "windows-old", "rubygems", "wandb",
                 })
        {
            Assert.NotNull(registry.Find(id));
        }

        Assert.Equal(120, registry.All.Count);
    }
}

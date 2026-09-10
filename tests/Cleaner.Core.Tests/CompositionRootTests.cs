using System.Text.RegularExpressions;
using Cleaner.Cli.Infrastructure;
using Cleaner.Core.Abstractions;
using Cleaner.Core.Cleaners;
using Cleaner.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cleaner.Core.Tests;

/// <summary>Audits cleaner registrations in the real composition root.</summary>
public sealed class CompositionRootTests
{
    private static ICleanerRegistry BuildRegistry()
    {
        var services = new ServiceCollection();
        services.AddCleaner();
        using var provider = services.BuildServiceProvider();
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

        Assert.All(registry.All, c => Assert.Contains(c.Category, Categories.Ordered));
        Assert.All(registry.All, c => Assert.NotEqual(CategoryGroups.Other, Categories.GroupOf(c.Category)));
        Assert.All(registry.All, c => Assert.False(string.IsNullOrWhiteSpace(c.Name)));
    }

    [Fact]
    public void Catalog_contains_the_expected_cleaners()
    {
        var registry = BuildRegistry();

        foreach (var id in new[]
                 {
                     "nuget", "npm", "uv", "conan", "zig", "julia", "podman", "helm", "pipx",
                     "corepack", "mise", "winget", "flatpak", "nix", "telegram", "gpu-installers",
                     "game-launchers", "unreal", "winsxs", "windows-old", "rubygems", "wandb",
                     "app-leftovers", "vscode-cpptools", "android-studio", "amd-telemetry",
                     "winre-agent", "razer", "claude-desktop", "codex", "docker-vhdx", "ngen-cache", "windows-installer-orphans",
                 })
        {
            Assert.NotNull(registry.Find(id));
        }

        Assert.Equal(131, registry.All.Count);
    }
}

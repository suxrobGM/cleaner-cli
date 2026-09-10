using Cleaner.Core.Cleaners;
using Xunit;

namespace Cleaner.Core.Tests;

/// <summary>The category-to-group layout the CLI lists and selects by.</summary>
public sealed class CategoriesTests
{
    [Fact]
    public void Every_category_belongs_to_a_known_group()
    {
        Assert.All(Categories.Ordered, c => Assert.Contains(Categories.GroupOf(c), Categories.Groups));
    }

    [Fact]
    public void Groups_are_listed_in_layout_order()
    {
        Assert.Equal(
            [CategoryGroups.OperatingSystem, CategoryGroups.Development, CategoryGroups.Applications],
            Categories.Groups);
    }

    [Fact]
    public void Categories_of_a_group_are_contiguous_in_the_display_order()
    {
        // The list renders one table per group, so a category ranked away from its own group would
        // split that table in two.
        var groups = Categories.Ordered.Select(Categories.GroupOf).ToList();
        Assert.Equal(Categories.Groups.Count, CountRuns(groups));
    }

    [Fact]
    public void Unknown_categories_land_in_Other_and_sort_last()
    {
        Assert.Equal(CategoryGroups.Other, Categories.GroupOf("Made up"));
        Assert.Equal(int.MaxValue, Categories.RankOf("Made up"));
        Assert.Equal(int.MaxValue, Categories.RankOfGroup("Made up"));
    }

    [Fact]
    public void RankOf_orders_categories_as_listed()
    {
        Assert.True(Categories.RankOf(Categories.SystemCaches) < Categories.RankOf(Categories.Dotnet));
        Assert.True(Categories.RankOf(Categories.Dotnet) < Categories.RankOf(Categories.DesktopApps));
    }

    private static int CountRuns(IReadOnlyList<string> values)
    {
        var runs = 0;
        for (var i = 0; i < values.Count; i++)
        {
            if (i == 0 || values[i] != values[i - 1])
            {
                runs++;
            }
        }

        return runs;
    }
}

namespace Cleaner.Core.Cleaners;

/// <summary>The top-level groups every category belongs to, for display and bulk selection.</summary>
public static class CategoryGroups
{
    public const string OperatingSystem = "Operating system";
    public const string Development = "Development tools";
    public const string Applications = "Applications";

    /// <summary>The group for a category that isn't in the known layout.</summary>
    public const string Other = "Other";
}

/// <summary>Canonical category names so grouping stays consistent across cleaners.</summary>
public static class Categories
{
    public const string SystemCaches = "System caches & temp";
    public const string SystemPackageManagers = "System package managers";
    public const string Dotnet = ".NET";
    public const string JavaScript = "JavaScript / TypeScript";
    public const string Python = "Python";
    public const string Jvm = "JVM / Android";
    public const string Rust = "Rust";
    public const string Go = "Go";
    public const string Languages = "Other languages";
    public const string MachineLearning = "Machine learning";
    public const string GameDev = "Game development";
    public const string Mobile = "Mobile (React Native / Expo)";
    public const string BuildCaches = "Build / monorepo caches";
    public const string Containers = "Containers / IaC";
    public const string Ides = "IDEs / editors";
    public const string ToolingDownloads = "Tooling downloads";
    public const string ProjectLocal = "Project-local";
    public const string DesktopApps = "Desktop apps";

    /// <summary>Categories in curated display order, paired with their groups.</summary>
    private static readonly (string Group, string Category)[] Layout =
    [
        (CategoryGroups.OperatingSystem, SystemCaches),
        (CategoryGroups.OperatingSystem, SystemPackageManagers),
        (CategoryGroups.Development, Dotnet),
        (CategoryGroups.Development, JavaScript),
        (CategoryGroups.Development, Python),
        (CategoryGroups.Development, Jvm),
        (CategoryGroups.Development, Rust),
        (CategoryGroups.Development, Go),
        (CategoryGroups.Development, Languages),
        (CategoryGroups.Development, MachineLearning),
        (CategoryGroups.Development, GameDev),
        (CategoryGroups.Development, Mobile),
        (CategoryGroups.Development, BuildCaches),
        (CategoryGroups.Development, Containers),
        (CategoryGroups.Development, Ides),
        (CategoryGroups.Development, ToolingDownloads),
        (CategoryGroups.Development, ProjectLocal),
        (CategoryGroups.Applications, DesktopApps),
    ];

    public static IReadOnlyList<string> Ordered { get; } = [.. Layout.Select(l => l.Category)];

    public static IReadOnlyList<string> Groups { get; } =
        [.. Layout.Select(l => l.Group).Distinct(StringComparer.Ordinal)];

    /// <summary>The group a category belongs to, or <see cref="CategoryGroups.Other"/> if unknown.</summary>
    public static string GroupOf(string category)
    {
        var rank = RankOf(category);
        return rank < Layout.Length ? Layout[rank].Group : CategoryGroups.Other;
    }

    /// <summary>Sort key matching <see cref="Ordered"/>; unknown categories sort last.</summary>
    public static int RankOf(string category) => IndexOf(Ordered, category);

    /// <summary>Sort key matching <see cref="Groups"/>; unknown groups sort last.</summary>
    public static int RankOfGroup(string group) => IndexOf(Groups, group);

    private static int IndexOf(IReadOnlyList<string> names, string name)
    {
        for (var i = 0; i < names.Count; i++)
        {
            if (string.Equals(names[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return int.MaxValue;
    }
}

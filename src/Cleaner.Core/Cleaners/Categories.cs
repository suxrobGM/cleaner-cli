namespace Cleaner.Core.Cleaners;

/// <summary>The three top-level buckets every category belongs to, for display and bulk selection.</summary>
public static class CategoryGroups
{
    public const string OperatingSystem = "Operating system";
    public const string Development = "Development tools";
    public const string Applications = "Applications";

    /// <summary>Where a category lands when it is not part of the known layout.</summary>
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

    /// <summary>
    /// Every category in the order the UI shows it, paired with its group. Reading order, not
    /// alphabetical: the OS buckets first, then development tooling from languages outward to the
    /// caches that surround them, then the everyday apps.
    /// </summary>
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

    /// <summary>Every known category, in display order.</summary>
    public static IReadOnlyList<string> Ordered { get; } = [.. Layout.Select(l => l.Category)];

    /// <summary>Every known group, in display order.</summary>
    public static IReadOnlyList<string> Groups { get; } =
        [.. Layout.Select(l => l.Group).Distinct(StringComparer.Ordinal)];

    /// <summary>The group a category belongs to, or <see cref="CategoryGroups.Other"/> if unknown.</summary>
    public static string GroupOf(string category)
    {
        var rank = RankOf(category);
        return rank < Layout.Length ? Layout[rank].Group : CategoryGroups.Other;
    }

    /// <summary>
    /// Sort key placing a category at its spot in <see cref="Ordered"/>. Unknown categories sort
    /// last as a block, where an alphabetical tie-break keeps them stable.
    /// </summary>
    public static int RankOf(string category) => IndexOf(Ordered, category);

    /// <summary>Sort key for a group, matching the order groups first appear in the layout.</summary>
    public static int RankOfGroup(string group) => IndexOf(Groups, group);

    /// <summary>Position of a name in a display-order list, or last when it is not in there.</summary>
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

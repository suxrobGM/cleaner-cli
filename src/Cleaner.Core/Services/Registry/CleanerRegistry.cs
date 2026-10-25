using Cleaner.Core.Abstractions;
using CategoryLayout = Cleaner.Core.Cleaners.Categories;

namespace Cleaner.Core.Services;

/// <inheritdoc cref="ICleanerRegistry"/>
public sealed class CleanerRegistry : ICleanerRegistry
{
    private readonly Dictionary<string, ICleaner> _byId;

    public CleanerRegistry(IEnumerable<ICleaner> cleaners)
    {
        // Curated category order (OS, then development tools, then apps) rather than alphabetical;
        // unknown categories sort last, alphabetically.
        All = cleaners
            .OrderBy(c => CategoryLayout.RankOf(c.Category))
            .ThenBy(c => c.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        _byId = All.ToDictionary(c => c.Id, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<ICleaner> All { get; }

    public ICleaner? Find(string id) =>
        _byId.GetValueOrDefault(id.Trim());
}

using Cleaner.Core.Abstractions;
using CategoryLayout = Cleaner.Core.Cleaners.Categories;

namespace Cleaner.Core.Services;

/// <inheritdoc cref="ICleanerRegistry"/>
public sealed class CleanerRegistry : ICleanerRegistry
{
    private readonly Dictionary<string, ICleaner> _byId;

    public CleanerRegistry(IEnumerable<ICleaner> cleaners)
    {
        // Categories run in their curated display order, not alphabetically, so the menu reads
        // top-down: OS first, then development tooling, then apps. Anything outside the known
        // layout sorts last as an alphabetical block.
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

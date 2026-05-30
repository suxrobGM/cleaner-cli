using Cleaner.Core.Abstractions;

namespace Cleaner.Core.Services;

/// <inheritdoc cref="ICleanerRegistry"/>
public sealed class CleanerRegistry : ICleanerRegistry
{
    private readonly Dictionary<string, ICleaner> _byId;

    public CleanerRegistry(IEnumerable<ICleaner> cleaners)
    {
        All = cleaners
            .OrderBy(c => c.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        _byId = All.ToDictionary(c => c.Id, StringComparer.OrdinalIgnoreCase);

        Categories = All
            .Select(c => c.Category)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyList<ICleaner> All { get; }

    public IReadOnlyList<string> Categories { get; }

    public ICleaner? Find(string id) =>
        _byId.GetValueOrDefault(id.Trim());

    public IReadOnlyList<ICleaner> InCategory(string category) =>
        All.Where(c => string.Equals(c.Category, category, StringComparison.OrdinalIgnoreCase)).ToArray();
}

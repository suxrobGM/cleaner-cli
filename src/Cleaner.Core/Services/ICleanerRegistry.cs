using Cleaner.Core.Abstractions;

namespace Cleaner.Core.Services;

/// <summary>Holds every registered cleaner and supports lookup by id and category.</summary>
public interface ICleanerRegistry
{
    IReadOnlyList<ICleaner> All { get; }

    /// <summary>Distinct categories in stable, sorted order.</summary>
    IReadOnlyList<string> Categories { get; }

    /// <summary>Find a cleaner by its <see cref="ICleaner.Id"/> (case-insensitive), or null.</summary>
    ICleaner? Find(string id);

    IReadOnlyList<ICleaner> InCategory(string category);
}

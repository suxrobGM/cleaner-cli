using Cleaner.Core.Abstractions;

namespace Cleaner.Core.Services;

/// <summary>Holds every registered cleaner and hands them out in display order.</summary>
public interface ICleanerRegistry
{
    IReadOnlyList<ICleaner> All { get; }

    /// <summary>Find a cleaner by its <see cref="ICleaner.Id"/> (case-insensitive), or null.</summary>
    ICleaner? Find(string id);
}

using Cleaner.Core.Abstractions;

namespace Cleaner.Core.Services;

/// <summary>Holds every registered cleaner and hands them out in display order.</summary>
public interface ICleanerRegistry
{
    IReadOnlyList<ICleaner> All { get; }

    /// <summary>Looks up <see cref="ICleaner.Id"/> case-insensitively; null when not found.</summary>
    ICleaner? Find(string id);
}

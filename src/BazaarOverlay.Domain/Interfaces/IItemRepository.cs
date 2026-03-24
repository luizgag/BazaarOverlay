using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;

namespace BazaarOverlay.Domain.Interfaces;

public interface IItemRepository
{
    Task<Item?> GetByNameAsync(string name);
    Task<IReadOnlyList<Item>> SearchByNameAsync(string partialName);
    Task<IReadOnlyList<Item>> GetByHeroAsync(string heroName);
    Task<IReadOnlyList<Item>> FilterAsync(string? heroName, ItemSize? maxSize, IEnumerable<string>? allowedTags, Rarity? maxRarity);
    Task AddAsync(Item item);
    Task UpdateAsync(Item item);
}

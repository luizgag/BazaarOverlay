using BazaarOverlay.Application.DTOs;
using BazaarOverlay.Application.Interfaces;
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace BazaarOverlay.Application.Services;

public class ItemInfoService : IItemInfoService
{
    private readonly IItemRepository _itemRepository;
    private readonly ILogger<ItemInfoService> _logger;

    public ItemInfoService(IItemRepository itemRepository, ILogger<ItemInfoService> logger)
    {
        _itemRepository = itemRepository;
        _logger = logger;
    }

    public async Task<ItemInfoResult?> GetItemInfoAsync(string itemName)
    {
        _logger.LogInformation("Looking up item: {ItemName}", itemName);
        var item = await _itemRepository.GetByNameAsync(itemName);
        if (item is null)
        {
            _logger.LogWarning("Item not found: {ItemName}", itemName);
            return null;
        }
        return MapToResult(item);
    }

    public async Task<IReadOnlyList<ItemInfoResult>> SearchItemsAsync(string partialName)
    {
        _logger.LogInformation("Searching items: {Query}", partialName);
        var items = await _itemRepository.SearchByNameAsync(partialName);
        return items.Select(MapToResult).ToList();
    }

    private static ItemInfoResult MapToResult(Item item)
    {
        return new ItemInfoResult(
            item.Name,
            item.Size,
            item.Cooldown,
            item.MinimumRarity,
            item.Tags.Select(t => t.Tag).ToList(),
            item.Heroes.Select(h => h.Name).ToList(),
            item.TierValues
                .OrderBy(tv => tv.Rarity)
                .Select(tv => new TierValueResult(tv.Rarity, tv.EffectDescription))
                .ToList(),
            item.Enchantments
                .Select(ie => new EnchantmentResult(ie.Enchantment.Name, ie.EffectDescription))
                .ToList()
        );
    }
}

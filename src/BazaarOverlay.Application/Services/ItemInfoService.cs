using BazaarOverlay.Application.DTOs;
using BazaarOverlay.Application.Interfaces;
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Interfaces;

namespace BazaarOverlay.Application.Services;

public class ItemInfoService : IItemInfoService
{
    private readonly IItemRepository _itemRepository;

    public ItemInfoService(IItemRepository itemRepository)
    {
        _itemRepository = itemRepository;
    }

    public async Task<ItemInfoResult?> GetItemInfoAsync(string itemName)
    {
        var item = await _itemRepository.GetByNameAsync(itemName);
        return item is null ? null : MapToResult(item);
    }

    public async Task<IReadOnlyList<ItemInfoResult>> SearchItemsAsync(string partialName)
    {
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

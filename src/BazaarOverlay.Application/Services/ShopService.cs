using BazaarOverlay.Application.DTOs;
using BazaarOverlay.Application.Interfaces;
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using BazaarOverlay.Domain.Interfaces;

namespace BazaarOverlay.Application.Services;

public class ShopService : IShopService
{
    private readonly IEncounterRepository _encounterRepository;
    private readonly IItemRepository _itemRepository;
    private readonly IRarityDayProbabilityRepository _rarityRepository;

    public ShopService(
        IEncounterRepository encounterRepository,
        IItemRepository itemRepository,
        IRarityDayProbabilityRepository rarityRepository)
    {
        _encounterRepository = encounterRepository;
        _itemRepository = itemRepository;
        _rarityRepository = rarityRepository;
    }

    public async Task<ShopResult?> GetShopItemsAsync(string shopName, string heroName, int currentDay)
    {
        var encounter = await _encounterRepository.GetByNameAsync(shopName);
        if (encounter is null || encounter.Type != EncounterType.Shop)
            return null;

        var items = await GetFilteredItemsAsync(encounter, heroName, currentDay);
        return MapToResult(encounter, items);
    }

    public async Task<IReadOnlyList<ShopResult>> SearchShopsAsync(string partialName, string heroName, int currentDay)
    {
        var encounters = await _encounterRepository.SearchByNameAsync(partialName);
        var shops = encounters.Where(e => e.Type == EncounterType.Shop).ToList();
        var results = new List<ShopResult>();

        foreach (var shop in shops)
        {
            var items = await GetFilteredItemsAsync(shop, heroName, currentDay);
            results.Add(MapToResult(shop, items));
        }

        return results;
    }

    private async Task<IReadOnlyList<Item>> GetFilteredItemsAsync(Encounter shop, string heroName, int currentDay)
    {
        var constraints = shop.ShopConstraints;
        var availableRarities = await _rarityRepository.GetAvailableRaritiesForDayAsync(currentDay);
        var maxRarity = availableRarities.Any() ? availableRarities.Max() : Rarity.Bronze;

        var heroFilter = constraints?.HeroOnly == true || shop.IsHeroSpecific ? heroName : null;
        var maxSize = constraints?.MaxSize;
        var maxRarityConstraint = constraints?.MaxRarity is not null
            ? (Rarity)Math.Min((int)constraints.MaxRarity, (int)maxRarity)
            : maxRarity;

        var allowedTags = shop.ShopAllowedTags.Any()
            ? shop.ShopAllowedTags.Select(t => t.Tag)
            : null;

        var items = await _itemRepository.FilterAsync(heroFilter, maxSize, allowedTags, maxRarityConstraint);

        if (constraints?.RequiredEnchantment is not null)
        {
            // For shops that require specific enchantments, we don't filter items by enchantment
            // since that's about the enchantment applied at the shop, not the item's possible enchantments
        }

        return items;
    }

    private static ShopResult MapToResult(Encounter shop, IReadOnlyList<Item> items)
    {
        return new ShopResult(
            shop.Name,
            shop.Description,
            items.Select(i => new ShopItemResult(
                i.Name,
                i.Size,
                i.MinimumRarity,
                i.Tags.Select(t => t.Tag).ToList()
            )).ToList()
        );
    }
}

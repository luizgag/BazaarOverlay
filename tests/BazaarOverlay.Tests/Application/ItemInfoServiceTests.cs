using BazaarOverlay.Application.Services;
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using BazaarOverlay.Domain.Interfaces;
using NSubstitute;
using Shouldly;

namespace BazaarOverlay.Tests.Application;

public class ItemInfoServiceTests
{
    private readonly IItemRepository _itemRepo = Substitute.For<IItemRepository>();
    private readonly ItemInfoService _sut;

    public ItemInfoServiceTests()
    {
        _sut = new ItemInfoService(_itemRepo);
    }

    [Fact]
    public async Task GetItemInfo_WithUnknownItem_ReturnsNull()
    {
        _itemRepo.GetByNameAsync("Unknown").Returns((Item?)null);

        var result = await _sut.GetItemInfoAsync("Unknown");

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetItemInfo_ReturnsCompleteItemInfo()
    {
        var item = CreateSwordWithDetails();
        _itemRepo.GetByNameAsync("Rusty Sword").Returns(item);

        var result = await _sut.GetItemInfoAsync("Rusty Sword");

        result.ShouldNotBeNull();
        result.Name.ShouldBe("Rusty Sword");
        result.Size.ShouldBe(ItemSize.Small);
        result.Cooldown.ShouldBe(4.0m);
        result.MinimumRarity.ShouldBe(Rarity.Bronze);
        result.Tags.ShouldContain("Weapon");
        result.Heroes.ShouldContain("Neutral");
        result.TierValues.Count.ShouldBe(3);
        result.TierValues[0].Rarity.ShouldBe(Rarity.Bronze);
        result.Enchantments.Count.ShouldBe(1);
        result.Enchantments[0].EnchantmentName.ShouldBe("Deadly");
    }

    [Fact]
    public async Task SearchItems_ReturnsMatchingResults()
    {
        var items = new List<Item> { CreateSwordWithDetails() };
        _itemRepo.SearchByNameAsync("Rust").Returns(items);

        var results = await _sut.SearchItemsAsync("Rust");

        results.Count.ShouldBe(1);
        results[0].Name.ShouldBe("Rusty Sword");
    }

    private static Item CreateSwordWithDetails()
    {
        var item = new Item("Rusty Sword", ItemSize.Small, Rarity.Bronze, 4.0m);
        var neutral = new Hero("Neutral");
        item.Heroes.Add(neutral);
        item.Tags.Add(new ItemTag("Weapon"));
        item.TierValues.Add(new ItemTierValue(Rarity.Bronze, "Deal 5 damage"));
        item.TierValues.Add(new ItemTierValue(Rarity.Silver, "Deal 10 damage"));
        item.TierValues.Add(new ItemTierValue(Rarity.Gold, "Deal 18 damage"));

        var enchantment = new Enchantment("Deadly", "+50% Crit Chance");
        item.Enchantments.Add(new ItemEnchantment(enchantment, "+50% Crit Chance"));

        return item;
    }
}

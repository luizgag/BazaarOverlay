using BazaarOverlay.Application.Services;
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using BazaarOverlay.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace BazaarOverlay.Tests.Application;

public class ShopServiceTests
{
    private readonly IEncounterRepository _encounterRepo = Substitute.For<IEncounterRepository>();
    private readonly IItemRepository _itemRepo = Substitute.For<IItemRepository>();
    private readonly IRarityDayProbabilityRepository _rarityRepo = Substitute.For<IRarityDayProbabilityRepository>();
    private readonly ShopService _sut;

    public ShopServiceTests()
    {
        _sut = new ShopService(_encounterRepo, _itemRepo, _rarityRepo, NullLogger<ShopService>.Instance);
    }

    [Fact]
    public async Task GetShopItems_WithUnknownShop_ReturnsNull()
    {
        _encounterRepo.GetByNameAsync("Unknown").Returns((Encounter?)null);

        var result = await _sut.GetShopItemsAsync("Unknown", "Vanessa", 1);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetShopItems_WithNonShopEncounter_ReturnsNull()
    {
        var monster = new Encounter("Goblin", Rarity.Bronze, "A goblin", EncounterType.Monster, false);
        _encounterRepo.GetByNameAsync("Goblin").Returns(monster);

        var result = await _sut.GetShopItemsAsync("Goblin", "Vanessa", 1);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetShopItems_FiltersItemsByHeroWhenHeroSpecific()
    {
        var shop = CreateCurioShop();
        _encounterRepo.GetByNameAsync("Curio").Returns(shop);
        _rarityRepo.GetAvailableRaritiesForDayAsync(1).Returns(new List<Rarity> { Rarity.Bronze });

        var items = new List<Item>
        {
            CreateItem("Rusty Sword", ItemSize.Small, Rarity.Bronze, "Weapon")
        };
        _itemRepo.FilterAsync("Vanessa", Arg.Any<ItemSize?>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<Rarity?>())
            .Returns(items);

        var result = await _sut.GetShopItemsAsync("Curio", "Vanessa", 1);

        result.ShouldNotBeNull();
        result.ShopName.ShouldBe("Curio");
        result.AvailableItems.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetShopItems_RespectsMaxSizeConstraint()
    {
        var shop = CreateSmallItemShop();
        _encounterRepo.GetByNameAsync("Small Shop").Returns(shop);
        _rarityRepo.GetAvailableRaritiesForDayAsync(1).Returns(new List<Rarity> { Rarity.Bronze });

        await _sut.GetShopItemsAsync("Small Shop", "Vanessa", 1);

        await _itemRepo.Received(1).FilterAsync(
            Arg.Any<string?>(),
            ItemSize.Small,
            Arg.Any<IEnumerable<string>?>(),
            Arg.Any<Rarity?>());
    }

    [Fact]
    public async Task SearchShops_ReturnsOnlyShopEncounters()
    {
        var encounters = new List<Encounter>
        {
            new Encounter("Curio", Rarity.Bronze, "Bronze items", EncounterType.Shop, true),
            new Encounter("Goblin Cave", Rarity.Bronze, "A goblin lair", EncounterType.Monster, false)
        };
        _encounterRepo.SearchByNameAsync("C").Returns(encounters);
        _rarityRepo.GetAvailableRaritiesForDayAsync(1).Returns(new List<Rarity> { Rarity.Bronze });
        _itemRepo.FilterAsync(Arg.Any<string?>(), Arg.Any<ItemSize?>(), Arg.Any<IEnumerable<string>?>(), Arg.Any<Rarity?>())
            .Returns(new List<Item>());

        var results = await _sut.SearchShopsAsync("C", "Vanessa", 1);

        results.Count.ShouldBe(1);
        results[0].ShopName.ShouldBe("Curio");
    }

    private static Encounter CreateCurioShop()
    {
        var shop = new Encounter("Curio", Rarity.Bronze, "Bronze unenchanted items", EncounterType.Shop, true);
        shop.SetShopConstraints(new ShopConstraints(null, Rarity.Bronze, null, true));
        return shop;
    }

    private static Encounter CreateSmallItemShop()
    {
        var shop = new Encounter("Small Shop", Rarity.Bronze, "Small items only", EncounterType.Shop, false);
        shop.SetShopConstraints(new ShopConstraints(ItemSize.Small, null, null, false));
        return shop;
    }

    private static Item CreateItem(string name, ItemSize size, Rarity rarity, string tag)
    {
        var item = new Item(name, size, rarity);
        item.Tags.Add(new ItemTag(tag));
        return item;
    }
}

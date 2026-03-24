using BazaarOverlay.Application.DTOs;
using BazaarOverlay.Application.Interfaces;
using BazaarOverlay.Application.Services;
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using BazaarOverlay.Domain.Interfaces;
using NSubstitute;
using Shouldly;

namespace BazaarOverlay.Tests.Application;

public class EncounterServiceTests
{
    private readonly IMonsterEncounterService _monsterService = Substitute.For<IMonsterEncounterService>();
    private readonly IShopService _shopService = Substitute.For<IShopService>();
    private readonly IEncounterRepository _encounterRepo = Substitute.For<IEncounterRepository>();
    private readonly IRarityDayProbabilityRepository _rarityRepo = Substitute.For<IRarityDayProbabilityRepository>();
    private readonly EncounterService _sut;

    public EncounterServiceTests()
    {
        _sut = new EncounterService(_monsterService, _shopService, _encounterRepo, _rarityRepo);
    }

    [Fact]
    public async Task SearchEncounters_WithMonsterType_DelegatesToMonsterService()
    {
        var encounter = new Encounter("Goblin", Rarity.Bronze, "A goblin", EncounterType.Monster, false);
        _encounterRepo.SearchByNameAsync("Gob").Returns(new List<Encounter> { encounter });
        _rarityRepo.GetAvailableRaritiesForDayAsync(1).Returns(new List<Rarity> { Rarity.Bronze });

        var monsterResult = new MonsterEncounterResult("Goblin", 45, Rarity.Bronze, 1,
            new List<DropResult>(), new List<DropResult>());
        _monsterService.GetMonsterEncounterAsync("Goblin", "Vanessa", 1).Returns(monsterResult);

        var results = await _sut.SearchEncountersAsync("Gob", "Vanessa", 1);

        results.Count.ShouldBe(1);
        results[0].Type.ShouldBe(EncounterType.Monster);
        results[0].MonsterDetails.ShouldNotBeNull();
        results[0].ShopDetails.ShouldBeNull();
    }

    [Fact]
    public async Task SearchEncounters_WithShopType_DelegatesToShopService()
    {
        var encounter = new Encounter("Curio", Rarity.Bronze, "Bronze items", EncounterType.Shop, true);
        _encounterRepo.SearchByNameAsync("Cur").Returns(new List<Encounter> { encounter });
        _rarityRepo.GetAvailableRaritiesForDayAsync(1).Returns(new List<Rarity> { Rarity.Bronze });

        var shopResult = new ShopResult("Curio", "Bronze items", new List<ShopItemResult>());
        _shopService.GetShopItemsAsync("Curio", "Vanessa", 1).Returns(shopResult);

        var results = await _sut.SearchEncountersAsync("Cur", "Vanessa", 1);

        results.Count.ShouldBe(1);
        results[0].Type.ShouldBe(EncounterType.Shop);
        results[0].ShopDetails.ShouldNotBeNull();
        results[0].MonsterDetails.ShouldBeNull();
    }

    [Fact]
    public async Task SearchEncounters_WithEventType_FiltersRewardsByHeroAndRarity()
    {
        var vanessa = new Hero("Vanessa");
        var neutral = new Hero("Neutral");

        var vanessaItem = new Item("Vanessa's Blade", ItemSize.Small, Rarity.Bronze);
        vanessaItem.Heroes.Add(vanessa);

        var neutralItem = new Item("Gold Coin", ItemSize.Small, Rarity.Bronze);
        neutralItem.Heroes.Add(neutral);

        var encounter = new Encounter("Treasure Chest", Rarity.Bronze, "Random loot", EncounterType.Event, true);
        encounter.RewardItems.Add(vanessaItem);
        encounter.RewardItems.Add(neutralItem);

        _encounterRepo.SearchByNameAsync("Treasure").Returns(new List<Encounter> { encounter });
        _rarityRepo.GetAvailableRaritiesForDayAsync(1).Returns(new List<Rarity> { Rarity.Bronze });

        var results = await _sut.SearchEncountersAsync("Treasure", "Vanessa", 1);

        results.Count.ShouldBe(1);
        results[0].Type.ShouldBe(EncounterType.Event);
        results[0].EventDetails.ShouldNotBeNull();
        results[0].EventDetails!.RewardItems.Count.ShouldBe(2);
    }

    [Fact]
    public async Task SearchEncounters_EventFiltersRarityByDay()
    {
        var neutral = new Hero("Neutral");
        var bronzeItem = new Item("Basic Sword", ItemSize.Small, Rarity.Bronze);
        bronzeItem.Heroes.Add(neutral);
        var goldItem = new Item("Golden Sword", ItemSize.Small, Rarity.Gold);
        goldItem.Heroes.Add(neutral);

        var encounter = new Encounter("Loot Box", Rarity.Bronze, "Random loot", EncounterType.Event, false);
        encounter.RewardItems.Add(bronzeItem);
        encounter.RewardItems.Add(goldItem);

        _encounterRepo.SearchByNameAsync("Loot").Returns(new List<Encounter> { encounter });
        // Day 1: only Bronze available
        _rarityRepo.GetAvailableRaritiesForDayAsync(1).Returns(new List<Rarity> { Rarity.Bronze });

        var results = await _sut.SearchEncountersAsync("Loot", "Vanessa", 1);

        results[0].EventDetails!.RewardItems.Count.ShouldBe(1);
        results[0].EventDetails!.RewardItems[0].Name.ShouldBe("Basic Sword");
    }
}

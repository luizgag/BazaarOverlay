using BazaarOverlay.Application.Services;
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using BazaarOverlay.Domain.Interfaces;
using NSubstitute;
using Shouldly;

namespace BazaarOverlay.Tests.Application;

public class MonsterEncounterServiceTests
{
    private readonly IMonsterRepository _monsterRepo = Substitute.For<IMonsterRepository>();
    private readonly IRarityDayProbabilityRepository _rarityRepo = Substitute.For<IRarityDayProbabilityRepository>();
    private readonly MonsterEncounterService _sut;

    public MonsterEncounterServiceTests()
    {
        _sut = new MonsterEncounterService(_monsterRepo, _rarityRepo);
    }

    [Fact]
    public async Task GetMonsterEncounter_WithUnknownMonster_ReturnsNull()
    {
        _monsterRepo.GetByNameAsync("Unknown").Returns((Monster?)null);

        var result = await _sut.GetMonsterEncounterAsync("Unknown", "Vanessa", 1);

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetMonsterEncounter_ReturnsMonsterWithFilteredDrops()
    {
        var monster = CreateGoblinWithDrops();
        _monsterRepo.GetByNameAsync("Goblin").Returns(monster);
        _rarityRepo.GetAvailableRaritiesForDayAsync(1).Returns(new List<Rarity> { Rarity.Bronze });

        var result = await _sut.GetMonsterEncounterAsync("Goblin", "Vanessa", 1);

        result.ShouldNotBeNull();
        result.MonsterName.ShouldBe("Goblin");
        result.Health.ShouldBe(45);
        result.Rarity.ShouldBe(Rarity.Bronze);
        result.ItemDrops.Count.ShouldBe(2); // Vanessa item + Neutral item (both Bronze)
    }

    [Fact]
    public async Task GetMonsterEncounter_FiltersItemsByHero()
    {
        var monster = CreateGoblinWithDrops();
        _monsterRepo.GetByNameAsync("Goblin").Returns(monster);
        _rarityRepo.GetAvailableRaritiesForDayAsync(1).Returns(new List<Rarity> { Rarity.Bronze, Rarity.Silver });

        var result = await _sut.GetMonsterEncounterAsync("Goblin", "Dooley", 1);

        result.ShouldNotBeNull();
        // Dooley should get Neutral items only, not Vanessa's item
        result.ItemDrops.ShouldNotContain(d => d.Name == "Vanessa's Blade");
        result.ItemDrops.Count.ShouldBe(2); // Generic Potion (Bronze) + Silver Shield (Silver, Neutral)
    }

    [Fact]
    public async Task GetMonsterEncounter_FiltersItemsByDayRarity()
    {
        var monster = CreateGoblinWithDrops();
        _monsterRepo.GetByNameAsync("Goblin").Returns(monster);
        // Day 1: only Bronze available
        _rarityRepo.GetAvailableRaritiesForDayAsync(1).Returns(new List<Rarity> { Rarity.Bronze });

        var result = await _sut.GetMonsterEncounterAsync("Goblin", "Vanessa", 1);

        result.ShouldNotBeNull();
        // Silver item should be filtered out
        result.ItemDrops.ShouldNotContain(d => d.MinimumRarity == Rarity.Silver);
    }

    [Fact]
    public async Task SearchMonsters_ReturnsMatchingResults()
    {
        var monsters = new List<Monster> { CreateGoblinWithDrops() };
        _monsterRepo.SearchByNameAsync("Gob").Returns(monsters);
        _rarityRepo.GetAvailableRaritiesForDayAsync(1).Returns(new List<Rarity> { Rarity.Bronze });

        var results = await _sut.SearchMonstersAsync("Gob", "Vanessa", 1);

        results.Count.ShouldBe(1);
        results[0].MonsterName.ShouldBe("Goblin");
    }

    [Fact]
    public async Task GetMonsterEncounter_FiltersSkillsByHero()
    {
        var monster = CreateGoblinWithDrops();
        _monsterRepo.GetByNameAsync("Goblin").Returns(monster);
        _rarityRepo.GetAvailableRaritiesForDayAsync(1).Returns(new List<Rarity> { Rarity.Bronze });

        var result = await _sut.GetMonsterEncounterAsync("Goblin", "Vanessa", 1);

        result.ShouldNotBeNull();
        result.SkillDrops.Count.ShouldBe(1);
        result.SkillDrops[0].Name.ShouldBe("Quick Strike");
    }

    private static Monster CreateGoblinWithDrops()
    {
        var vanessa = new Hero("Vanessa");
        var neutral = new Hero("Neutral");

        var vanessaItem = new Item("Vanessa's Blade", ItemSize.Small, Rarity.Bronze);
        vanessaItem.Heroes.Add(vanessa);

        var neutralItem = new Item("Generic Potion", ItemSize.Small, Rarity.Bronze);
        neutralItem.Heroes.Add(neutral);

        var silverItem = new Item("Silver Shield", ItemSize.Medium, Rarity.Silver);
        silverItem.Heroes.Add(neutral);

        var vanessaSkill = new Skill("Quick Strike", Rarity.Bronze);
        vanessaSkill.Heroes.Add(vanessa);

        var dooleySkill = new Skill("Heavy Slam", Rarity.Bronze);
        dooleySkill.Heroes.Add(new Hero("Dooley"));

        var monster = new Monster("Goblin", 45, Rarity.Bronze, 1);
        monster.DropItems.Add(vanessaItem);
        monster.DropItems.Add(neutralItem);
        monster.DropItems.Add(silverItem);
        monster.DropSkills.Add(vanessaSkill);
        monster.DropSkills.Add(dooleySkill);

        return monster;
    }
}

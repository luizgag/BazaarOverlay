using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using BazaarOverlay.Infrastructure.Persistence.Repositories;
using Shouldly;

namespace BazaarOverlay.Tests.Infrastructure;

public class MonsterRepositoryTests : IDisposable
{
    private readonly BazaarOverlay.Infrastructure.Persistence.BazaarDbContext _context;
    private readonly MonsterRepository _sut;

    public MonsterRepositoryTests()
    {
        _context = TestDbContextFactory.Create();
        _sut = new MonsterRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task GetByName_WithExistingMonster_ReturnsMonsterWithDrops()
    {
        await SeedGoblinAsync();

        var result = await _sut.GetByNameAsync("Goblin");

        result.ShouldNotBeNull();
        result.Name.ShouldBe("Goblin");
        result.DropItems.Count.ShouldBe(1);
        result.DropItems.First().Name.ShouldBe("Rusty Sword");
        result.DropItems.First().Heroes.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetByName_WithUnknownMonster_ReturnsNull()
    {
        var result = await _sut.GetByNameAsync("Unknown");

        result.ShouldBeNull();
    }

    [Fact]
    public async Task SearchByName_WithPartialMatch_ReturnsResults()
    {
        await SeedGoblinAsync();

        var results = await _sut.SearchByNameAsync("Gob");

        results.Count.ShouldBe(1);
        results[0].Name.ShouldBe("Goblin");
    }

    [Fact]
    public async Task SearchByName_IsCaseInsensitive()
    {
        await SeedGoblinAsync();

        var results = await _sut.SearchByNameAsync("goblin");

        results.Count.ShouldBe(1);
    }

    [Fact]
    public async Task SearchByName_RanksExactMatchFirst()
    {
        var goblin = new Monster("Goblin", 45, Rarity.Bronze, 1);
        var goblinKing = new Monster("Goblin King", 120, Rarity.Gold, 5);
        await _context.Monsters.AddRangeAsync(goblin, goblinKing);
        await _context.SaveChangesAsync();

        var results = await _sut.SearchByNameAsync("Goblin");

        results[0].Name.ShouldBe("Goblin");
        results[1].Name.ShouldBe("Goblin King");
    }

    [Fact]
    public async Task GetByDay_ReturnsMonstersThatAppearOnOrBeforeDay()
    {
        var earlyMonster = new Monster("Rat", 20, Rarity.Bronze, 1);
        var lateMonster = new Monster("Dragon", 500, Rarity.Diamond, 8);
        await _context.Monsters.AddRangeAsync(earlyMonster, lateMonster);
        await _context.SaveChangesAsync();

        var results = await _sut.GetByDayAsync(5);

        results.Count.ShouldBe(1);
        results[0].Name.ShouldBe("Rat");
    }

    private async Task SeedGoblinAsync()
    {
        var neutral = new Hero("Neutral");
        _context.Heroes.Add(neutral);

        var item = new Item("Rusty Sword", ItemSize.Small, Rarity.Bronze, 4.0m);
        item.Heroes.Add(neutral);

        var monster = new Monster("Goblin", 45, Rarity.Bronze, 1);
        monster.DropItems.Add(item);

        await _context.Monsters.AddAsync(monster);
        await _context.SaveChangesAsync();
    }
}

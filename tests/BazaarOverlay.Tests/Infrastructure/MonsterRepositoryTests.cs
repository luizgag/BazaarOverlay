using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using BazaarOverlay.Infrastructure.Persistence.Repositories;
using BazaarOverlay.Tests.Infrastructure;
using Shouldly;

namespace BazaarOverlay.Tests.Infrastructure;

public class MonsterRepositoryTests : IDisposable
{
    private readonly BazaarOverlay.Infrastructure.Persistence.BazaarDbContext _context;
    private readonly MonsterRepository _repository;

    public MonsterRepositoryTests()
    {
        _context = TestDbContextFactory.Create();
        _repository = new MonsterRepository(_context);
    }

    [Fact]
    public async Task GetByNameAsync_ExistingMonster_ReturnsMonster()
    {
        _context.Monsters.Add(new Monster("Banannibal", Rarity.Bronze, level: 1, day: 1,
            health: 100, goldReward: 2, xpReward: 3));
        await _context.SaveChangesAsync();

        var result = await _repository.GetByNameAsync("Banannibal");

        result.ShouldNotBeNull();
        result.Name.ShouldBe("Banannibal");
    }

    [Fact]
    public async Task GetByNameAsync_WithBoardItems_ReturnsBoardItems()
    {
        var item = new Item("Banana", ItemSize.Small, Rarity.Bronze);
        _context.Items.Add(item);
        await _context.SaveChangesAsync();

        var monster = new Monster("Banannibal", Rarity.Bronze, level: 1, day: 1,
            health: 100, goldReward: 2, xpReward: 3);
        monster.BoardItems.Add(item);
        _context.Monsters.Add(monster);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByNameAsync("Banannibal");

        result.ShouldNotBeNull();
        result.BoardItems.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetByDayAsync_ReturnsOnlyMonstersUpToDay()
    {
        _context.Monsters.Add(new Monster("Day1Monster", Rarity.Bronze, level: 1, day: 1,
            health: 100, goldReward: 2, xpReward: 3));
        _context.Monsters.Add(new Monster("Day3Monster", Rarity.Silver, level: 3, day: 3,
            health: 300, goldReward: 4, xpReward: 5));
        await _context.SaveChangesAsync();

        var result = await _repository.GetByDayAsync(2);

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Day1Monster");
    }

    [Fact]
    public async Task SearchByNameAsync_PartialMatch_ReturnsResults()
    {
        _context.Monsters.Add(new Monster("Banannibal", Rarity.Bronze, level: 1, day: 1,
            health: 100, goldReward: 2, xpReward: 3));
        _context.Monsters.Add(new Monster("Fanged Inglet", Rarity.Bronze, level: 1, day: 1,
            health: 80, goldReward: 2, xpReward: 3));
        await _context.SaveChangesAsync();

        var result = await _repository.SearchByNameAsync("banan");

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Banannibal");
    }

    public void Dispose() => _context.Dispose();
}

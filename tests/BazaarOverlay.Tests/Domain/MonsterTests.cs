using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using Shouldly;

namespace BazaarOverlay.Tests.Domain;

public class MonsterTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesMonster()
    {
        var monster = new Monster("Banannibal", Rarity.Bronze, level: 1, day: 1,
            health: 100, goldReward: 2, xpReward: 3);

        monster.Name.ShouldBe("Banannibal");
        monster.Tier.ShouldBe(Rarity.Bronze);
        monster.Level.ShouldBe(1);
        monster.Day.ShouldBe(1);
        monster.Health.ShouldBe(100);
        monster.GoldReward.ShouldBe(2);
        monster.XpReward.ShouldBe(3);
    }

    [Fact]
    public void Constructor_WithBazaarDbId_SetsId()
    {
        var monster = new Monster("Banannibal", Rarity.Bronze, level: 1, day: 1,
            health: 100, goldReward: 2, xpReward: 3,
            bazaarDbId: "4k4n5d9g1c9ydpt7c1gy7wg72q");

        monster.BazaarDbId.ShouldBe("4k4n5d9g1c9ydpt7c1gy7wg72q");
    }

    [Fact]
    public void Constructor_TrimsName()
    {
        var monster = new Monster("  Banannibal  ", Rarity.Bronze, level: 1, day: 1,
            health: 100, goldReward: 2, xpReward: 3);

        monster.Name.ShouldBe("Banannibal");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidName_ThrowsArgumentException(string? name)
    {
        Should.Throw<ArgumentException>(() => new Monster(name!, Rarity.Bronze,
            level: 1, day: 1, health: 100, goldReward: 2, xpReward: 3));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithInvalidHealth_ThrowsArgumentException(int health)
    {
        Should.Throw<ArgumentException>(() => new Monster("Goblin", Rarity.Bronze,
            level: 1, day: 1, health: health, goldReward: 2, xpReward: 3));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithInvalidDay_ThrowsArgumentException(int day)
    {
        Should.Throw<ArgumentException>(() => new Monster("Goblin", Rarity.Bronze,
            level: 1, day: day, health: 100, goldReward: 2, xpReward: 3));
    }

    [Fact]
    public void BoardCollections_StartEmpty()
    {
        var monster = new Monster("Banannibal", Rarity.Bronze, level: 1, day: 1,
            health: 100, goldReward: 2, xpReward: 3);

        monster.BoardItems.ShouldBeEmpty();
        monster.BoardSkills.ShouldBeEmpty();
    }
}

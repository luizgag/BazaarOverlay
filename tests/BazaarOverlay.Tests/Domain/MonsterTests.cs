using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using Shouldly;

namespace BazaarOverlay.Tests.Domain;

public class MonsterTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesMonster()
    {
        var monster = new Monster("Goblin", 45, Rarity.Bronze, 1);

        monster.Name.ShouldBe("Goblin");
        monster.Health.ShouldBe(45);
        monster.Rarity.ShouldBe(Rarity.Bronze);
        monster.FirstDay.ShouldBe(1);
    }

    [Fact]
    public void Constructor_TrimsName()
    {
        var monster = new Monster("  Goblin  ", 45, Rarity.Bronze, 1);

        monster.Name.ShouldBe("Goblin");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidName_ThrowsArgumentException(string? name)
    {
        Should.Throw<ArgumentException>(() => new Monster(name!, 45, Rarity.Bronze, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithInvalidHealth_ThrowsArgumentException(int health)
    {
        Should.Throw<ArgumentException>(() => new Monster("Goblin", health, Rarity.Bronze, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithInvalidFirstDay_ThrowsArgumentException(int firstDay)
    {
        Should.Throw<ArgumentException>(() => new Monster("Goblin", 45, Rarity.Bronze, firstDay));
    }

    [Fact]
    public void AppearsOnDay_OnFirstDay_ReturnsTrue()
    {
        var monster = new Monster("Goblin", 45, Rarity.Bronze, 3);

        monster.AppearsOnDay(3).ShouldBeTrue();
    }

    [Fact]
    public void AppearsOnDay_AfterFirstDay_ReturnsTrue()
    {
        var monster = new Monster("Goblin", 45, Rarity.Bronze, 3);

        monster.AppearsOnDay(5).ShouldBeTrue();
    }

    [Fact]
    public void AppearsOnDay_BeforeFirstDay_ReturnsFalse()
    {
        var monster = new Monster("Goblin", 45, Rarity.Bronze, 3);

        monster.AppearsOnDay(2).ShouldBeFalse();
    }
}

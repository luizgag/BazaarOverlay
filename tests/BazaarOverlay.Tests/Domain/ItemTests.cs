using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using Shouldly;

namespace BazaarOverlay.Tests.Domain;

public class ItemTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesItem()
    {
        var item = new Item("Rusty Sword", ItemSize.Small, Rarity.Bronze, cooldown: 4.0m);

        item.Name.ShouldBe("Rusty Sword");
        item.Size.ShouldBe(ItemSize.Small);
        item.MinimumRarity.ShouldBe(Rarity.Bronze);
        item.Cooldown.ShouldBe(4.0m);
    }

    [Fact]
    public void Constructor_WithNullCooldown_AllowsNull()
    {
        var item = new Item("Shield", ItemSize.Medium, Rarity.Bronze);

        item.Cooldown.ShouldBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidName_ThrowsArgumentException(string? name)
    {
        Should.Throw<ArgumentException>(() => new Item(name!, ItemSize.Small, Rarity.Bronze));
    }

    [Fact]
    public void IsAvailableForHero_WithMatchingHero_ReturnsTrue()
    {
        var item = new Item("Vanessa's Blade", ItemSize.Small, Rarity.Bronze);
        var hero = new Hero("Vanessa");
        item.Heroes.Add(hero);

        item.IsAvailableForHero("Vanessa").ShouldBeTrue();
    }

    [Fact]
    public void IsAvailableForHero_WithNeutralHero_ReturnsTrue()
    {
        var item = new Item("Generic Sword", ItemSize.Small, Rarity.Bronze);
        var neutral = new Hero("Neutral");
        item.Heroes.Add(neutral);

        item.IsAvailableForHero("Vanessa").ShouldBeTrue();
    }

    [Fact]
    public void IsAvailableForHero_WithDifferentHero_ReturnsFalse()
    {
        var item = new Item("Vanessa's Blade", ItemSize.Small, Rarity.Bronze);
        var hero = new Hero("Vanessa");
        item.Heroes.Add(hero);

        item.IsAvailableForHero("Dooley").ShouldBeFalse();
    }

    [Fact]
    public void IsAvailableOnDay_WithPositiveProbability_ReturnsTrue()
    {
        var item = new Item("Rusty Sword", ItemSize.Small, Rarity.Bronze);
        var probabilities = new[] { new RarityDayProbability(1, Rarity.Bronze, 100.0m) };

        item.IsAvailableOnDay(1, probabilities).ShouldBeTrue();
    }

    [Fact]
    public void IsAvailableOnDay_WithZeroProbability_ReturnsFalse()
    {
        var item = new Item("Diamond Ring", ItemSize.Small, Rarity.Diamond);
        var probabilities = new[] { new RarityDayProbability(1, Rarity.Diamond, 0.0m) };

        item.IsAvailableOnDay(1, probabilities).ShouldBeFalse();
    }
}

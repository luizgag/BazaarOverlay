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
    public void Constructor_WithAllFields_SetsAllProperties()
    {
        var item = new Item("Trapping Pit", ItemSize.Medium, Rarity.Silver,
            cooldown: 6.0m, cost: "8 >> 16 >> 32", value: "4 >> 8 >> 16",
            description: "Destroy enemy item", bazaarDbId: "py8ycyzvx7yg6xk05lj73jdyx");

        item.Cost.ShouldBe("8 >> 16 >> 32");
        item.Value.ShouldBe("4 >> 8 >> 16");
        item.Description.ShouldBe("Destroy enemy item");
        item.BazaarDbId.ShouldBe("py8ycyzvx7yg6xk05lj73jdyx");
    }

    [Fact]
    public void Constructor_WithNullOptionalFields_AllowsNulls()
    {
        var item = new Item("Shield", ItemSize.Medium, Rarity.Bronze);

        item.Cooldown.ShouldBeNull();
        item.Cost.ShouldBeNull();
        item.Value.ShouldBeNull();
        item.Description.ShouldBeNull();
        item.BazaarDbId.ShouldBeNull();
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
}

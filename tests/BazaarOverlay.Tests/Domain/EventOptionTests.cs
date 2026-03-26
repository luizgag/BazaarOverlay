using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using Shouldly;

namespace BazaarOverlay.Tests.Domain;

public class EventOptionTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesEventOption()
    {
        var option = new EventOption("Trade It for Something", Rarity.Bronze,
            description: "Gain a Neutral item");

        option.Name.ShouldBe("Trade It for Something");
        option.Tier.ShouldBe(Rarity.Bronze);
        option.Description.ShouldBe("Gain a Neutral item");
        option.HeroRestriction.ShouldBeNull();
    }

    [Fact]
    public void Constructor_WithHeroRestriction_SetsRestriction()
    {
        var option = new EventOption("Brew a Potion", Rarity.Bronze,
            description: "Gain a potion", heroRestriction: "Mak");

        option.HeroRestriction.ShouldBe("Mak");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidName_ThrowsArgumentException(string? name)
    {
        Should.Throw<ArgumentException>(() => new EventOption(name!, Rarity.Bronze, "desc"));
    }
}

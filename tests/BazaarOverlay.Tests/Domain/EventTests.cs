using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using Shouldly;

namespace BazaarOverlay.Tests.Domain;

public class EventTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesEvent()
    {
        var evt = new Event("A Strange Mushroom", Rarity.Bronze,
            tooltip: "You find a strange mushroom.",
            bazaarDbId: "10y2n43q8sd46dcj7k38j5w25vs");

        evt.Name.ShouldBe("A Strange Mushroom");
        evt.Tier.ShouldBe(Rarity.Bronze);
        evt.Tooltip.ShouldBe("You find a strange mushroom.");
        evt.BazaarDbId.ShouldBe("10y2n43q8sd46dcj7k38j5w25vs");
    }

    [Fact]
    public void Constructor_WithRules_SetsAllFields()
    {
        var evt = new Event("A Strange Mushroom", Rarity.Bronze,
            tooltip: "You find a mushroom.",
            selectionRule: "You can only select one card.",
            costRule: "You must pay the cost.",
            leaveRule: "You can leave.");

        evt.SelectionRule.ShouldBe("You can only select one card.");
        evt.CostRule.ShouldBe("You must pay the cost.");
        evt.LeaveRule.ShouldBe("You can leave.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidName_ThrowsArgumentException(string? name)
    {
        Should.Throw<ArgumentException>(() => new Event(name!, Rarity.Bronze));
    }

    [Fact]
    public void Options_StartsEmpty()
    {
        var evt = new Event("A Strange Mushroom", Rarity.Bronze);

        evt.Options.ShouldBeEmpty();
    }
}

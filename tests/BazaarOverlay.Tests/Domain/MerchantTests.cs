using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using Shouldly;

namespace BazaarOverlay.Tests.Domain;

public class MerchantTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesMerchant()
    {
        var merchant = new Merchant("Barkun", Rarity.Silver,
            tooltip: "Sells Medium and Large items.",
            bazaarDbId: "qf6h07kp9vmw5mcym5m3wdtbny");

        merchant.Name.ShouldBe("Barkun");
        merchant.Tier.ShouldBe(Rarity.Silver);
        merchant.Tooltip.ShouldBe("Sells Medium and Large items.");
        merchant.BazaarDbId.ShouldBe("qf6h07kp9vmw5mcym5m3wdtbny");
    }

    [Fact]
    public void Constructor_WithRules_SetsAllFields()
    {
        var merchant = new Merchant("Barkun", Rarity.Silver,
            tooltip: "Sells items",
            selectionRule: "You are able to select multiple items.",
            costRule: "You must pay the cost.",
            leaveRule: "You can leave.",
            rerollCount: 1, rerollCost: 4);

        merchant.SelectionRule.ShouldBe("You are able to select multiple items.");
        merchant.CostRule.ShouldBe("You must pay the cost.");
        merchant.LeaveRule.ShouldBe("You can leave.");
        merchant.RerollCount.ShouldBe(1);
        merchant.RerollCost.ShouldBe(4);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidName_ThrowsArgumentException(string? name)
    {
        Should.Throw<ArgumentException>(() => new Merchant(name!, Rarity.Silver, "tooltip"));
    }

    [Fact]
    public void ItemPool_StartsEmpty()
    {
        var merchant = new Merchant("Barkun", Rarity.Silver, tooltip: "Sells items");

        merchant.ItemPool.ShouldBeEmpty();
    }
}

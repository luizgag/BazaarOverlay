using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using Shouldly;

namespace BazaarOverlay.Tests.Domain;

public class TrainerTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesTrainer()
    {
        var trainer = new Trainer("Adira", Rarity.Diamond,
            tooltip: "Teaches Diamond-tier Skills",
            bazaarDbId: "w99h14w3m1sfljzmd2ldfglnh6");

        trainer.Name.ShouldBe("Adira");
        trainer.Tier.ShouldBe(Rarity.Diamond);
        trainer.Tooltip.ShouldBe("Teaches Diamond-tier Skills");
        trainer.BazaarDbId.ShouldBe("w99h14w3m1sfljzmd2ldfglnh6");
    }

    [Fact]
    public void Constructor_WithRules_SetsAllFields()
    {
        var trainer = new Trainer("Adira", Rarity.Diamond,
            tooltip: "Teaches skills",
            selectionRule: "You can only select one skill.",
            costRule: "Skills are always free.",
            leaveRule: "You can leave.",
            rerollCount: 1, rerollCost: 8);

        trainer.SelectionRule.ShouldBe("You can only select one skill.");
        trainer.RerollCount.ShouldBe(1);
        trainer.RerollCost.ShouldBe(8);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidName_ThrowsArgumentException(string? name)
    {
        Should.Throw<ArgumentException>(() => new Trainer(name!, Rarity.Diamond, "tooltip"));
    }

    [Fact]
    public void SkillPool_StartsEmpty()
    {
        var trainer = new Trainer("Adira", Rarity.Diamond, tooltip: "Teaches skills");

        trainer.SkillPool.ShouldBeEmpty();
    }
}

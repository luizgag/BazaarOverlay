using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using Shouldly;

namespace BazaarOverlay.Tests.Domain;

public class SkillTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesSkill()
    {
        var skill = new Skill("Burning Temper", Rarity.Bronze);

        skill.Name.ShouldBe("Burning Temper");
        skill.MinimumRarity.ShouldBe(Rarity.Bronze);
    }

    [Fact]
    public void Constructor_WithAllFields_SetsAllProperties()
    {
        var skill = new Skill("Burning Temper", Rarity.Bronze,
            cost: "5 >> 10 >> 20 >> 40",
            description: "While Enraged, Burn items have +3 Burn",
            bazaarDbId: "304tdnf7npqk1kbshhxx48p3fj");

        skill.Cost.ShouldBe("5 >> 10 >> 20 >> 40");
        skill.Description.ShouldBe("While Enraged, Burn items have +3 Burn");
        skill.BazaarDbId.ShouldBe("304tdnf7npqk1kbshhxx48p3fj");
    }

    [Fact]
    public void Constructor_WithNullOptionalFields_AllowsNulls()
    {
        var skill = new Skill("Burning Temper", Rarity.Bronze);

        skill.Cost.ShouldBeNull();
        skill.Description.ShouldBeNull();
        skill.BazaarDbId.ShouldBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidName_ThrowsArgumentException(string? name)
    {
        Should.Throw<ArgumentException>(() => new Skill(name!, Rarity.Bronze));
    }
}

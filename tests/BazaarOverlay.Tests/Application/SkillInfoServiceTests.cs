using BazaarOverlay.Application.Services;
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using BazaarOverlay.Domain.Interfaces;
using NSubstitute;
using Shouldly;

namespace BazaarOverlay.Tests.Application;

public class SkillInfoServiceTests
{
    private readonly ISkillRepository _skillRepo = Substitute.For<ISkillRepository>();
    private readonly SkillInfoService _sut;

    public SkillInfoServiceTests()
    {
        _sut = new SkillInfoService(_skillRepo);
    }

    [Fact]
    public async Task GetSkillInfo_WithUnknownSkill_ReturnsNull()
    {
        _skillRepo.GetByNameAsync("Unknown").Returns((Skill?)null);

        var result = await _sut.GetSkillInfoAsync("Unknown");

        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetSkillInfo_ReturnsCompleteSkillInfo()
    {
        var skill = CreateQuickStrike();
        _skillRepo.GetByNameAsync("Quick Strike").Returns(skill);

        var result = await _sut.GetSkillInfoAsync("Quick Strike");

        result.ShouldNotBeNull();
        result.Name.ShouldBe("Quick Strike");
        result.MinimumRarity.ShouldBe(Rarity.Bronze);
        result.Tags.ShouldContain("Damage");
        result.Heroes.ShouldContain("Vanessa");
        result.TierValues.Count.ShouldBe(2);
        result.TierValues[0].Rarity.ShouldBe(Rarity.Bronze);
        result.TierValues[0].EffectDescription.ShouldBe("Deal 3 damage");
    }

    [Fact]
    public async Task SearchSkills_ReturnsMatchingResults()
    {
        var skills = new List<Skill> { CreateQuickStrike() };
        _skillRepo.SearchByNameAsync("Quick").Returns(skills);

        var results = await _sut.SearchSkillsAsync("Quick");

        results.Count.ShouldBe(1);
        results[0].Name.ShouldBe("Quick Strike");
    }

    private static Skill CreateQuickStrike()
    {
        var skill = new Skill("Quick Strike", Rarity.Bronze);
        skill.Heroes.Add(new Hero("Vanessa"));
        skill.Tags.Add(new SkillTag("Damage"));
        skill.TierValues.Add(new SkillTierValue(Rarity.Bronze, "Deal 3 damage"));
        skill.TierValues.Add(new SkillTierValue(Rarity.Silver, "Deal 6 damage"));
        return skill;
    }
}

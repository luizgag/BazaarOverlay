using BazaarOverlay.Application.DTOs;
using BazaarOverlay.Application.Interfaces;
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Interfaces;

namespace BazaarOverlay.Application.Services;

public class SkillInfoService : ISkillInfoService
{
    private readonly ISkillRepository _skillRepository;

    public SkillInfoService(ISkillRepository skillRepository)
    {
        _skillRepository = skillRepository;
    }

    public async Task<SkillInfoResult?> GetSkillInfoAsync(string skillName)
    {
        var skill = await _skillRepository.GetByNameAsync(skillName);
        return skill is null ? null : MapToResult(skill);
    }

    public async Task<IReadOnlyList<SkillInfoResult>> SearchSkillsAsync(string partialName)
    {
        var skills = await _skillRepository.SearchByNameAsync(partialName);
        return skills.Select(MapToResult).ToList();
    }

    private static SkillInfoResult MapToResult(Skill skill)
    {
        return new SkillInfoResult(
            skill.Name,
            skill.MinimumRarity,
            skill.Tags.Select(t => t.Tag).ToList(),
            skill.Heroes.Select(h => h.Name).ToList(),
            skill.TierValues
                .OrderBy(tv => tv.Rarity)
                .Select(tv => new TierValueResult(tv.Rarity, tv.EffectDescription))
                .ToList()
        );
    }
}

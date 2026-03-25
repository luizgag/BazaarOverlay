using BazaarOverlay.Application.DTOs;
using BazaarOverlay.Application.Interfaces;
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace BazaarOverlay.Application.Services;

public class SkillInfoService : ISkillInfoService
{
    private readonly ISkillRepository _skillRepository;
    private readonly ILogger<SkillInfoService> _logger;

    public SkillInfoService(ISkillRepository skillRepository, ILogger<SkillInfoService> logger)
    {
        _skillRepository = skillRepository;
        _logger = logger;
    }

    public async Task<SkillInfoResult?> GetSkillInfoAsync(string skillName)
    {
        _logger.LogInformation("Looking up skill: {SkillName}", skillName);
        var skill = await _skillRepository.GetByNameAsync(skillName);
        if (skill is null)
        {
            _logger.LogWarning("Skill not found: {SkillName}", skillName);
            return null;
        }
        return MapToResult(skill);
    }

    public async Task<IReadOnlyList<SkillInfoResult>> SearchSkillsAsync(string partialName)
    {
        _logger.LogInformation("Searching skills: {Query}", partialName);
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

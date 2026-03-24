using BazaarOverlay.Application.DTOs;

namespace BazaarOverlay.Application.Interfaces;

public interface ISkillInfoService
{
    Task<SkillInfoResult?> GetSkillInfoAsync(string skillName);
    Task<IReadOnlyList<SkillInfoResult>> SearchSkillsAsync(string partialName);
}

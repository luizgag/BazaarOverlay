using BazaarOverlay.Domain.Entities;

namespace BazaarOverlay.Domain.Interfaces;

public interface ISkillRepository
{
    Task<Skill?> GetByNameAsync(string name);
    Task<IReadOnlyList<Skill>> SearchByNameAsync(string partialName);
    Task AddAsync(Skill skill);
    Task UpdateAsync(Skill skill);
}

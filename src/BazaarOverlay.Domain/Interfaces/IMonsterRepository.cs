using BazaarOverlay.Domain.Entities;

namespace BazaarOverlay.Domain.Interfaces;

public interface IMonsterRepository
{
    Task<Monster?> GetByNameAsync(string name);
    Task<IReadOnlyList<Monster>> SearchByNameAsync(string partialName);
    Task<IReadOnlyList<Monster>> GetByDayAsync(int day);
    Task AddAsync(Monster monster);
}

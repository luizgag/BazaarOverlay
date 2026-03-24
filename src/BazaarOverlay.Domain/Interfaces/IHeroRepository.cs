using BazaarOverlay.Domain.Entities;

namespace BazaarOverlay.Domain.Interfaces;

public interface IHeroRepository
{
    Task<IReadOnlyList<Hero>> GetAllAsync();
    Task<Hero?> GetByNameAsync(string name);
    Task AddAsync(Hero hero);
}

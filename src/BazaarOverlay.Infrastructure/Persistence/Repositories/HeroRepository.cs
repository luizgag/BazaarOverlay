using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BazaarOverlay.Infrastructure.Persistence.Repositories;

public class HeroRepository : IHeroRepository
{
    private readonly BazaarDbContext _context;

    public HeroRepository(BazaarDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Hero>> GetAllAsync()
    {
        return await _context.Heroes.OrderBy(h => h.Name).ToListAsync();
    }

    public async Task<Hero?> GetByNameAsync(string name)
    {
        return await _context.Heroes.FindAsync(name);
    }

    public async Task AddAsync(Hero hero)
    {
        await _context.Heroes.AddAsync(hero);
        await _context.SaveChangesAsync();
    }
}

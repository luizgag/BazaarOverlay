using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BazaarOverlay.Infrastructure.Persistence.Repositories;

public class MonsterRepository : IMonsterRepository
{
    private readonly BazaarDbContext _context;

    public MonsterRepository(BazaarDbContext context)
    {
        _context = context;
    }

    public async Task<Monster?> GetByNameAsync(string name)
    {
        return await _context.Monsters
            .Include(m => m.DropItems).ThenInclude(i => i.Heroes)
            .Include(m => m.DropSkills).ThenInclude(s => s.Heroes)
            .FirstOrDefaultAsync(m => m.Name == name);
    }

    public async Task<IReadOnlyList<Monster>> SearchByNameAsync(string partialName)
    {
        var lower = partialName.ToLower();

        var monsters = await _context.Monsters
            .Include(m => m.DropItems).ThenInclude(i => i.Heroes)
            .Include(m => m.DropSkills).ThenInclude(s => s.Heroes)
            .Where(m => m.Name.ToLower().Contains(lower))
            .ToListAsync();

        return monsters
            .OrderBy(m => m.Name.Equals(partialName, StringComparison.OrdinalIgnoreCase) ? 0
                        : m.Name.StartsWith(partialName, StringComparison.OrdinalIgnoreCase) ? 1
                        : 2)
            .ThenBy(m => m.Name)
            .ToList();
    }

    public async Task<IReadOnlyList<Monster>> GetByDayAsync(int day)
    {
        return await _context.Monsters
            .Include(m => m.DropItems).ThenInclude(i => i.Heroes)
            .Include(m => m.DropSkills).ThenInclude(s => s.Heroes)
            .Where(m => m.FirstDay <= day)
            .ToListAsync();
    }

    public async Task AddAsync(Monster monster)
    {
        await _context.Monsters.AddAsync(monster);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Monster monster)
    {
        _context.Monsters.Update(monster);
        await _context.SaveChangesAsync();
    }
}

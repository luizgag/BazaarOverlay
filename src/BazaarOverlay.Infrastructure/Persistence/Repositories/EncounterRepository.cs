using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BazaarOverlay.Infrastructure.Persistence.Repositories;

public class EncounterRepository : IEncounterRepository
{
    private readonly BazaarDbContext _context;

    public EncounterRepository(BazaarDbContext context)
    {
        _context = context;
    }

    public async Task<Encounter?> GetByNameAsync(string name)
    {
        return await FullQuery()
            .FirstOrDefaultAsync(e => e.Name == name);
    }

    public async Task<IReadOnlyList<Encounter>> SearchByNameAsync(string partialName)
    {
        var lower = partialName.ToLower();

        var encounters = await FullQuery()
            .Where(e => e.Name.ToLower().Contains(lower))
            .ToListAsync();

        return encounters
            .OrderBy(e => e.Name.Equals(partialName, StringComparison.OrdinalIgnoreCase) ? 0
                        : e.Name.StartsWith(partialName, StringComparison.OrdinalIgnoreCase) ? 1
                        : 2)
            .ThenBy(e => e.Name)
            .ToList();
    }

    public async Task AddAsync(Encounter encounter)
    {
        await _context.Encounters.AddAsync(encounter);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Encounter encounter)
    {
        _context.Encounters.Update(encounter);
        await _context.SaveChangesAsync();
    }

    private IQueryable<Encounter> FullQuery()
    {
        return _context.Encounters
            .Include(e => e.ShopConstraints)
            .Include(e => e.ShopAllowedTags)
            .Include(e => e.RewardItems).ThenInclude(i => i.Heroes)
            .Include(e => e.RewardItems).ThenInclude(i => i.Tags)
            .Include(e => e.RewardSkills).ThenInclude(s => s.Heroes);
    }
}

using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BazaarOverlay.Infrastructure.Persistence.Repositories;

public class SkillRepository : ISkillRepository
{
    private readonly BazaarDbContext _context;

    public SkillRepository(BazaarDbContext context)
    {
        _context = context;
    }

    public async Task<Skill?> GetByNameAsync(string name)
    {
        return await FullQuery()
            .FirstOrDefaultAsync(s => s.Name == name);
    }

    public async Task<IReadOnlyList<Skill>> SearchByNameAsync(string partialName)
    {
        var lower = partialName.ToLower();

        var skills = await FullQuery()
            .Where(s => s.Name.ToLower().Contains(lower))
            .ToListAsync();

        return skills
            .OrderBy(s => s.Name.Equals(partialName, StringComparison.OrdinalIgnoreCase) ? 0
                        : s.Name.StartsWith(partialName, StringComparison.OrdinalIgnoreCase) ? 1
                        : 2)
            .ThenBy(s => s.Name)
            .ToList();
    }

    public async Task AddAsync(Skill skill)
    {
        await _context.Skills.AddAsync(skill);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Skill skill)
    {
        _context.Skills.Update(skill);
        await _context.SaveChangesAsync();
    }

    private IQueryable<Skill> FullQuery()
    {
        return _context.Skills
            .Include(s => s.Heroes)
            .Include(s => s.Tags)
            .Include(s => s.TierValues);
    }
}

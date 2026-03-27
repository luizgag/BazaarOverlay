using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BazaarOverlay.Infrastructure.Persistence.Repositories;

public class TrainerRepository : ITrainerRepository
{
    private readonly BazaarDbContext _context;

    public TrainerRepository(BazaarDbContext context)
    {
        _context = context;
    }

    public async Task<Trainer?> GetByNameAsync(string name)
    {
        return await _context.Trainers
            .Include(t => t.SkillPool)
            .FirstOrDefaultAsync(t => t.Name == name);
    }

    public async Task<IReadOnlyList<Trainer>> SearchByNameAsync(string partialName)
    {
        var lower = partialName.ToLower();

        var trainers = await _context.Trainers
            .Include(t => t.SkillPool)
            .Where(t => t.Name.ToLower().Contains(lower))
            .ToListAsync();

        return trainers
            .OrderBy(t => t.Name.Equals(partialName, StringComparison.OrdinalIgnoreCase) ? 0
                        : t.Name.StartsWith(partialName, StringComparison.OrdinalIgnoreCase) ? 1
                        : 2)
            .ThenBy(t => t.Name)
            .ToList();
    }

    public async Task AddAsync(Trainer trainer)
    {
        await _context.Trainers.AddAsync(trainer);
        await _context.SaveChangesAsync();
    }
}

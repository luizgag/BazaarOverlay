using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using BazaarOverlay.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BazaarOverlay.Infrastructure.Persistence.Repositories;

public class RarityDayProbabilityRepository : IRarityDayProbabilityRepository
{
    private readonly BazaarDbContext _context;

    public RarityDayProbabilityRepository(BazaarDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<RarityDayProbability>> GetByDayAsync(int day)
    {
        return await _context.RarityDayProbabilities
            .Where(p => p.Day == day)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Rarity>> GetAvailableRaritiesForDayAsync(int day)
    {
        return await _context.RarityDayProbabilities
            .Where(p => p.Day == day && p.Probability > 0)
            .Select(p => p.Rarity)
            .ToListAsync();
    }

    public async Task AddRangeAsync(IEnumerable<RarityDayProbability> probabilities)
    {
        await _context.RarityDayProbabilities.AddRangeAsync(probabilities);
        await _context.SaveChangesAsync();
    }
}

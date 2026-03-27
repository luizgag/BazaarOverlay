using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BazaarOverlay.Infrastructure.Persistence.Repositories;

public class CardUrlCacheRepository : ICardUrlCacheRepository
{
    private readonly BazaarDbContext _context;

    public CardUrlCacheRepository(BazaarDbContext context)
    {
        _context = context;
    }

    public async Task<CardUrlCache?> GetByNameAsync(string name)
    {
        return await _context.CardUrlCaches
            .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());
    }

    public async Task SaveAsync(CardUrlCache entry)
    {
        await _context.CardUrlCaches.AddAsync(entry);
        await _context.SaveChangesAsync();
    }
}

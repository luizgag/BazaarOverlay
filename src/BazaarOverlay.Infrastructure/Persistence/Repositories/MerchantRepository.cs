using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BazaarOverlay.Infrastructure.Persistence.Repositories;

public class MerchantRepository : IMerchantRepository
{
    private readonly BazaarDbContext _context;

    public MerchantRepository(BazaarDbContext context)
    {
        _context = context;
    }

    public async Task<Merchant?> GetByNameAsync(string name)
    {
        return await _context.Merchants
            .Include(m => m.ItemPool)
            .FirstOrDefaultAsync(m => m.Name == name);
    }

    public async Task<IReadOnlyList<Merchant>> SearchByNameAsync(string partialName)
    {
        var lower = partialName.ToLower();

        var merchants = await _context.Merchants
            .Include(m => m.ItemPool)
            .Where(m => m.Name.ToLower().Contains(lower))
            .ToListAsync();

        return merchants
            .OrderBy(m => m.Name.Equals(partialName, StringComparison.OrdinalIgnoreCase) ? 0
                        : m.Name.StartsWith(partialName, StringComparison.OrdinalIgnoreCase) ? 1
                        : 2)
            .ThenBy(m => m.Name)
            .ToList();
    }

    public async Task AddAsync(Merchant merchant)
    {
        await _context.Merchants.AddAsync(merchant);
        await _context.SaveChangesAsync();
    }
}

using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using BazaarOverlay.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BazaarOverlay.Infrastructure.Persistence.Repositories;

public class ItemRepository : IItemRepository
{
    private readonly BazaarDbContext _context;

    public ItemRepository(BazaarDbContext context)
    {
        _context = context;
    }

    public async Task<Item?> GetByNameAsync(string name)
    {
        return await FullQuery()
            .FirstOrDefaultAsync(i => i.Name == name);
    }

    public async Task<IReadOnlyList<Item>> SearchByNameAsync(string partialName)
    {
        var lower = partialName.ToLower();

        var items = await FullQuery()
            .Where(i => i.Name.ToLower().Contains(lower))
            .ToListAsync();

        return items
            .OrderBy(i => i.Name.Equals(partialName, StringComparison.OrdinalIgnoreCase) ? 0
                        : i.Name.StartsWith(partialName, StringComparison.OrdinalIgnoreCase) ? 1
                        : 2)
            .ThenBy(i => i.Name)
            .ToList();
    }

    public async Task<IReadOnlyList<Item>> GetByHeroAsync(string heroName)
    {
        return await FullQuery()
            .Where(i => i.Heroes.Any(h => h.Name == heroName || h.Name == "Neutral"))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Item>> FilterAsync(
        string? heroName, ItemSize? maxSize, IEnumerable<string>? allowedTags, Rarity? maxRarity)
    {
        var query = FullQuery();

        if (heroName is not null)
            query = query.Where(i => i.Heroes.Any(h => h.Name == heroName || h.Name == "Neutral"));

        if (maxSize is not null)
            query = query.Where(i => i.Size <= maxSize.Value);

        if (maxRarity is not null)
            query = query.Where(i => i.MinimumRarity <= maxRarity.Value);

        var items = await query.ToListAsync();

        if (allowedTags is not null)
        {
            var tagSet = allowedTags.ToHashSet(StringComparer.OrdinalIgnoreCase);
            items = items.Where(i => i.Tags.Any(t => tagSet.Contains(t.Tag))).ToList();
        }

        return items;
    }

    public async Task AddAsync(Item item)
    {
        await _context.Items.AddAsync(item);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Item item)
    {
        _context.Items.Update(item);
        await _context.SaveChangesAsync();
    }

    private IQueryable<Item> FullQuery()
    {
        return _context.Items
            .Include(i => i.Heroes)
            .Include(i => i.Tags)
            .Include(i => i.TierValues)
            .Include(i => i.Enchantments).ThenInclude(ie => ie.Enchantment);
    }
}

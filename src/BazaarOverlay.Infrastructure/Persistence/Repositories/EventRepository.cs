using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BazaarOverlay.Infrastructure.Persistence.Repositories;

public class EventRepository : IEventRepository
{
    private readonly BazaarDbContext _context;

    public EventRepository(BazaarDbContext context)
    {
        _context = context;
    }

    public async Task<Event?> GetByNameAsync(string name)
    {
        return await _context.Events
            .Include(e => e.Options)
            .FirstOrDefaultAsync(e => e.Name == name);
    }

    public async Task<IReadOnlyList<Event>> SearchByNameAsync(string partialName)
    {
        var lower = partialName.ToLower();

        var events = await _context.Events
            .Include(e => e.Options)
            .Where(e => e.Name.ToLower().Contains(lower))
            .ToListAsync();

        return events
            .OrderBy(e => e.Name.Equals(partialName, StringComparison.OrdinalIgnoreCase) ? 0
                        : e.Name.StartsWith(partialName, StringComparison.OrdinalIgnoreCase) ? 1
                        : 2)
            .ThenBy(e => e.Name)
            .ToList();
    }

    public async Task AddAsync(Event evt)
    {
        await _context.Events.AddAsync(evt);
        await _context.SaveChangesAsync();
    }
}

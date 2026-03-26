using BazaarOverlay.Domain.Entities;

namespace BazaarOverlay.Domain.Interfaces;

public interface IEventRepository
{
    Task<Event?> GetByNameAsync(string name);
    Task<IReadOnlyList<Event>> SearchByNameAsync(string partialName);
    Task AddAsync(Event evt);
}

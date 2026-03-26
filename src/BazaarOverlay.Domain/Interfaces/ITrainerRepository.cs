using BazaarOverlay.Domain.Entities;

namespace BazaarOverlay.Domain.Interfaces;

public interface ITrainerRepository
{
    Task<Trainer?> GetByNameAsync(string name);
    Task<IReadOnlyList<Trainer>> SearchByNameAsync(string partialName);
    Task AddAsync(Trainer trainer);
}

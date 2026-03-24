using BazaarOverlay.Domain.Entities;

namespace BazaarOverlay.Domain.Interfaces;

public interface IEncounterRepository
{
    Task<Encounter?> GetByNameAsync(string name);
    Task<IReadOnlyList<Encounter>> SearchByNameAsync(string partialName);
    Task AddAsync(Encounter encounter);
    Task UpdateAsync(Encounter encounter);
}

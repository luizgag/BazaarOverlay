using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;

namespace BazaarOverlay.Domain.Interfaces;

public interface IRarityDayProbabilityRepository
{
    Task<IReadOnlyList<RarityDayProbability>> GetByDayAsync(int day);
    Task<IReadOnlyList<Rarity>> GetAvailableRaritiesForDayAsync(int day);
    Task AddRangeAsync(IEnumerable<RarityDayProbability> probabilities);
}

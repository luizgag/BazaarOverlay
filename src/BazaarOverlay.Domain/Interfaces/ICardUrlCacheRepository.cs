using BazaarOverlay.Domain.Entities;

namespace BazaarOverlay.Domain.Interfaces;

public interface ICardUrlCacheRepository
{
    Task<CardUrlCache?> GetByNameAsync(string name);
    Task SaveAsync(CardUrlCache entry);
}

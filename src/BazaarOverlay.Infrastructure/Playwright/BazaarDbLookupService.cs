using BazaarOverlay.Application.Interfaces;
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Interfaces;

namespace BazaarOverlay.Infrastructure.Playwright;

public class BazaarDbLookupService : IBazaarDbLookupService
{
    private readonly ICardUrlCacheRepository _cacheRepository;
    private readonly IPlaywrightSearchService _searchService;

    public BazaarDbLookupService(
        ICardUrlCacheRepository cacheRepository,
        IPlaywrightSearchService searchService)
    {
        _cacheRepository = cacheRepository;
        _searchService = searchService;
    }

    public async Task<string?> GetCardUrlAsync(string name)
    {
        var cached = await _cacheRepository.GetByNameAsync(name);
        if (cached is not null)
            return cached.CardUrl;

        var (cardUrl, category) = await _searchService.SearchAsync(name);
        if (cardUrl is null)
            return null;

        var entry = new CardUrlCache(name, cardUrl, category);
        await _cacheRepository.SaveAsync(entry);

        return cardUrl;
    }
}

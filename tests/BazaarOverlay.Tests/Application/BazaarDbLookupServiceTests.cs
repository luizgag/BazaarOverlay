using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Interfaces;
using BazaarOverlay.Infrastructure.Playwright;
using NSubstitute;
using Shouldly;

namespace BazaarOverlay.Tests.Application;

public class BazaarDbLookupServiceTests
{
    private readonly ICardUrlCacheRepository _cacheRepo = Substitute.For<ICardUrlCacheRepository>();
    private readonly IPlaywrightSearchService _searchService = Substitute.For<IPlaywrightSearchService>();
    private readonly BazaarDbLookupService _service;

    public BazaarDbLookupServiceTests()
    {
        _service = new BazaarDbLookupService(_cacheRepo, _searchService);
    }

    [Fact]
    public async Task GetCardUrlAsync_CacheHit_ReturnsCachedUrl()
    {
        var cached = new CardUrlCache("Pigomorph", "https://bazaardb.gg/items/pigomorph", "items");
        _cacheRepo.GetByNameAsync("Pigomorph").Returns(cached);

        var result = await _service.GetCardUrlAsync("Pigomorph");

        result.ShouldBe("https://bazaardb.gg/items/pigomorph");
        await _searchService.DidNotReceive().SearchAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task GetCardUrlAsync_CacheMiss_SearchesAndCaches()
    {
        _cacheRepo.GetByNameAsync("Pigomorph").Returns((CardUrlCache?)null);
        _searchService.SearchAsync("Pigomorph").Returns(("https://bazaardb.gg/items/pigomorph", "items"));

        var result = await _service.GetCardUrlAsync("Pigomorph");

        result.ShouldBe("https://bazaardb.gg/items/pigomorph");
        await _cacheRepo.Received(1).SaveAsync(Arg.Is<CardUrlCache>(c =>
            c.Name == "Pigomorph" &&
            c.CardUrl == "https://bazaardb.gg/items/pigomorph" &&
            c.Category == "items"));
    }

    [Fact]
    public async Task GetCardUrlAsync_SearchReturnsNull_ReturnsNull()
    {
        _cacheRepo.GetByNameAsync("Unknown").Returns((CardUrlCache?)null);
        _searchService.SearchAsync("Unknown").Returns(((string?)null, (string?)null));

        var result = await _service.GetCardUrlAsync("Unknown");

        result.ShouldBeNull();
        await _cacheRepo.DidNotReceive().SaveAsync(Arg.Any<CardUrlCache>());
    }
}

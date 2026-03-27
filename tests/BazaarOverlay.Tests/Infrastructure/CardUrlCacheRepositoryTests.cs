using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Infrastructure.Persistence.Repositories;
using BazaarOverlay.Tests.Infrastructure;
using Shouldly;

namespace BazaarOverlay.Tests.Infrastructure;

public class CardUrlCacheRepositoryTests : IDisposable
{
    private readonly BazaarOverlay.Infrastructure.Persistence.BazaarDbContext _context;
    private readonly CardUrlCacheRepository _repository;

    public CardUrlCacheRepositoryTests()
    {
        _context = TestDbContextFactory.Create();
        _repository = new CardUrlCacheRepository(_context);
    }

    [Fact]
    public async Task GetByNameAsync_ExistingEntry_ReturnsEntry()
    {
        _context.CardUrlCaches.Add(new CardUrlCache("Pigomorph", "/card/123/pigomorph", "Item"));
        await _context.SaveChangesAsync();

        var result = await _repository.GetByNameAsync("Pigomorph");

        result.ShouldNotBeNull();
        result.Name.ShouldBe("Pigomorph");
        result.CardUrl.ShouldBe("/card/123/pigomorph");
    }

    [Fact]
    public async Task GetByNameAsync_CaseInsensitive_ReturnsEntry()
    {
        _context.CardUrlCaches.Add(new CardUrlCache("Pigomorph", "/card/123/pigomorph", "Item"));
        await _context.SaveChangesAsync();

        var result = await _repository.GetByNameAsync("pigomorph");

        result.ShouldNotBeNull();
        result.Name.ShouldBe("Pigomorph");
    }

    [Fact]
    public async Task GetByNameAsync_NotFound_ReturnsNull()
    {
        var result = await _repository.GetByNameAsync("NonExistent");

        result.ShouldBeNull();
    }

    [Fact]
    public async Task SaveAsync_NewEntry_PersistsToDatabase()
    {
        var entry = new CardUrlCache("Pigomorph", "/card/123/pigomorph", "Item");

        await _repository.SaveAsync(entry);

        var saved = await _context.CardUrlCaches.FindAsync(entry.Id);
        saved.ShouldNotBeNull();
        saved.Name.ShouldBe("Pigomorph");
    }

    public void Dispose() => _context.Dispose();
}

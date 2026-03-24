using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using BazaarOverlay.Infrastructure.Persistence.Repositories;
using Shouldly;

namespace BazaarOverlay.Tests.Infrastructure;

public class RarityDayProbabilityRepositoryTests : IDisposable
{
    private readonly BazaarOverlay.Infrastructure.Persistence.BazaarDbContext _context;
    private readonly RarityDayProbabilityRepository _sut;

    public RarityDayProbabilityRepositoryTests()
    {
        _context = TestDbContextFactory.Create();
        _sut = new RarityDayProbabilityRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task GetAvailableRaritiesForDay_ReturnsOnlyPositiveProbabilities()
    {
        await _sut.AddRangeAsync(new[]
        {
            new RarityDayProbability(1, Rarity.Bronze, 100.0m),
            new RarityDayProbability(1, Rarity.Silver, 0.0m),
            new RarityDayProbability(1, Rarity.Gold, 0.0m),
        });

        var result = await _sut.GetAvailableRaritiesForDayAsync(1);

        result.Count.ShouldBe(1);
        result.ShouldContain(Rarity.Bronze);
    }

    [Fact]
    public async Task GetByDay_ReturnsAllProbabilitiesForDay()
    {
        await _sut.AddRangeAsync(new[]
        {
            new RarityDayProbability(5, Rarity.Bronze, 30.0m),
            new RarityDayProbability(5, Rarity.Silver, 55.0m),
            new RarityDayProbability(5, Rarity.Gold, 15.0m),
        });

        var result = await _sut.GetByDayAsync(5);

        result.Count.ShouldBe(3);
    }
}

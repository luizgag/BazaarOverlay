using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using BazaarOverlay.Infrastructure.Persistence.Repositories;
using BazaarOverlay.Tests.Infrastructure;
using Shouldly;

namespace BazaarOverlay.Tests.Infrastructure;

public class EventRepositoryTests : IDisposable
{
    private readonly BazaarOverlay.Infrastructure.Persistence.BazaarDbContext _context;
    private readonly EventRepository _repository;

    public EventRepositoryTests()
    {
        _context = TestDbContextFactory.Create();
        _repository = new EventRepository(_context);
    }

    [Fact]
    public async Task GetByNameAsync_ExistingEvent_ReturnsWithOptions()
    {
        var evt = new Event("A Strange Mushroom", Rarity.Bronze, tooltip: "You find a mushroom.");
        evt.Options.Add(new EventOption("Trade It", Rarity.Bronze, description: "Gain a Neutral item"));
        evt.Options.Add(new EventOption("Sell It", Rarity.Bronze, description: "Gain 4 Gold."));
        _context.Events.Add(evt);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByNameAsync("A Strange Mushroom");

        result.ShouldNotBeNull();
        result.Options.Count.ShouldBe(2);
    }

    [Fact]
    public async Task SearchByNameAsync_PartialMatch_ReturnsResults()
    {
        _context.Events.Add(new Event("Jungle Ruins", Rarity.Bronze));
        _context.Events.Add(new Event("A Strange Mushroom", Rarity.Bronze));
        await _context.SaveChangesAsync();

        var result = await _repository.SearchByNameAsync("jungle");

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Jungle Ruins");
    }

    public void Dispose() => _context.Dispose();
}

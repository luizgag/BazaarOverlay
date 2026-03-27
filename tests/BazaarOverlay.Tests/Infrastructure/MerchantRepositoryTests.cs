using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using BazaarOverlay.Infrastructure.Persistence.Repositories;
using BazaarOverlay.Tests.Infrastructure;
using Shouldly;

namespace BazaarOverlay.Tests.Infrastructure;

public class MerchantRepositoryTests : IDisposable
{
    private readonly BazaarOverlay.Infrastructure.Persistence.BazaarDbContext _context;
    private readonly MerchantRepository _repository;

    public MerchantRepositoryTests()
    {
        _context = TestDbContextFactory.Create();
        _repository = new MerchantRepository(_context);
    }

    [Fact]
    public async Task GetByNameAsync_ExistingMerchant_ReturnsMerchant()
    {
        _context.Merchants.Add(new Merchant("Barkun", Rarity.Silver, tooltip: "Sells items"));
        await _context.SaveChangesAsync();

        var result = await _repository.GetByNameAsync("Barkun");

        result.ShouldNotBeNull();
        result.Name.ShouldBe("Barkun");
        result.Tier.ShouldBe(Rarity.Silver);
    }

    [Fact]
    public async Task GetByNameAsync_WithItemPool_ReturnsItemPool()
    {
        var item = new Item("Big Sword", ItemSize.Large, Rarity.Silver);
        _context.Items.Add(item);
        await _context.SaveChangesAsync();

        var merchant = new Merchant("Barkun", Rarity.Silver, tooltip: "Sells items");
        merchant.ItemPool.Add(item);
        _context.Merchants.Add(merchant);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByNameAsync("Barkun");

        result.ShouldNotBeNull();
        result.ItemPool.Count.ShouldBe(1);
    }

    [Fact]
    public async Task SearchByNameAsync_PartialMatch_ReturnsResults()
    {
        _context.Merchants.Add(new Merchant("Barkun", Rarity.Silver, tooltip: "Sells items"));
        _context.Merchants.Add(new Merchant("Aero", Rarity.Bronze, tooltip: "Sells vehicles"));
        await _context.SaveChangesAsync();

        var result = await _repository.SearchByNameAsync("bar");

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Barkun");
    }

    public void Dispose() => _context.Dispose();
}

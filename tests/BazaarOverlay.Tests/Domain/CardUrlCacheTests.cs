using BazaarOverlay.Domain.Entities;
using Shouldly;

namespace BazaarOverlay.Tests.Domain;

public class CardUrlCacheTests
{
    [Fact]
    public void Constructor_ValidData_SetsProperties()
    {
        var cache = new CardUrlCache("Pigomorph", "/card/123/pigomorph", "Item");

        cache.Name.ShouldBe("Pigomorph");
        cache.CardUrl.ShouldBe("/card/123/pigomorph");
        cache.Category.ShouldBe("Item");
        cache.CachedAt.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-5), DateTime.UtcNow);
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        var cache = new CardUrlCache("  Pigomorph  ", "/card/123/pigomorph ", " Item ");

        cache.Name.ShouldBe("Pigomorph");
        cache.CardUrl.ShouldBe("/card/123/pigomorph");
        cache.Category.ShouldBe("Item");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyName_Throws(string? name)
    {
        Should.Throw<ArgumentException>(() => new CardUrlCache(name!, "/card/123/pigomorph", "Item"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyCardUrl_Throws(string? cardUrl)
    {
        Should.Throw<ArgumentException>(() => new CardUrlCache("Pigomorph", cardUrl!, "Item"));
    }
}

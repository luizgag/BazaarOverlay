using BazaarOverlay.Domain.Entities;
using Shouldly;

namespace BazaarOverlay.Tests.Domain;

public class HeroTests
{
    [Fact]
    public void Constructor_WithValidName_CreatesHero()
    {
        var hero = new Hero("Vanessa");

        hero.Name.ShouldBe("Vanessa");
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        var hero = new Hero("  Vanessa  ");

        hero.Name.ShouldBe("Vanessa");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidName_ThrowsArgumentException(string? name)
    {
        Should.Throw<ArgumentException>(() => new Hero(name!));
    }
}

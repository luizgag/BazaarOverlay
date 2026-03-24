using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using Shouldly;

namespace BazaarOverlay.Tests.Domain;

public class RarityDayProbabilityTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesInstance()
    {
        var prob = new RarityDayProbability(1, Rarity.Bronze, 100.0m);

        prob.Day.ShouldBe(1);
        prob.Rarity.ShouldBe(Rarity.Bronze);
        prob.Probability.ShouldBe(100.0m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithInvalidDay_ThrowsArgumentException(int day)
    {
        Should.Throw<ArgumentException>(() => new RarityDayProbability(day, Rarity.Bronze, 50.0m));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Constructor_WithInvalidProbability_ThrowsArgumentException(decimal probability)
    {
        Should.Throw<ArgumentException>(() => new RarityDayProbability(1, Rarity.Bronze, probability));
    }
}

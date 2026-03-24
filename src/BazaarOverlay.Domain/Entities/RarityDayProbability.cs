using System.ComponentModel.DataAnnotations;
using BazaarOverlay.Domain.Enums;

namespace BazaarOverlay.Domain.Entities;

public class RarityDayProbability
{
    [Required]
    [Range(1, int.MaxValue)]
    public int Day { get; private set; }

    [Required]
    public Rarity Rarity { get; private set; }

    [Required]
    [Range(0, 100)]
    public decimal Probability { get; private set; }

    private RarityDayProbability() { }

    public RarityDayProbability(int day, Rarity rarity, decimal probability)
    {
        if (day <= 0)
            throw new ArgumentException("Day must be positive.", nameof(day));
        if (probability < 0 || probability > 100)
            throw new ArgumentException("Probability must be between 0 and 100.", nameof(probability));

        Day = day;
        Rarity = rarity;
        Probability = probability;
    }
}

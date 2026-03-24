using System.ComponentModel.DataAnnotations;
using BazaarOverlay.Domain.Enums;

namespace BazaarOverlay.Domain.Entities;

public class Item
{
    [Key]
    public int Id { get; private set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; private set; } = string.Empty;

    [Required]
    public ItemSize Size { get; private set; }

    public decimal? Cooldown { get; private set; }

    [Required]
    public Rarity MinimumRarity { get; private set; }

    public ICollection<ItemTag> Tags { get; private set; } = new List<ItemTag>();
    public ICollection<Hero> Heroes { get; private set; } = new List<Hero>();
    public ICollection<ItemTierValue> TierValues { get; private set; } = new List<ItemTierValue>();
    public ICollection<ItemEnchantment> Enchantments { get; private set; } = new List<ItemEnchantment>();

    private Item() { }

    public Item(string name, ItemSize size, Rarity minimumRarity, decimal? cooldown = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Item name cannot be empty.", nameof(name));

        Name = name.Trim();
        Size = size;
        MinimumRarity = minimumRarity;
        Cooldown = cooldown;
    }

    public bool IsAvailableForHero(string heroName)
    {
        return Heroes.Any(h => h.Name == heroName || h.Name == "Neutral");
    }

    public bool IsAvailableOnDay(int day, IEnumerable<RarityDayProbability> probabilities)
    {
        return probabilities.Any(p => p.Day == day && p.Rarity == MinimumRarity && p.Probability > 0);
    }
}

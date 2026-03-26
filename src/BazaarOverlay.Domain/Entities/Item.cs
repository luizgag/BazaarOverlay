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

    [MaxLength(100)]
    public string? Cost { get; private set; }

    [MaxLength(100)]
    public string? Value { get; private set; }

    [MaxLength(500)]
    public string? Description { get; private set; }

    [MaxLength(50)]
    public string? BazaarDbId { get; private set; }

    public ICollection<ItemTag> Tags { get; private set; } = new List<ItemTag>();
    public ICollection<Hero> Heroes { get; private set; } = new List<Hero>();
    public ICollection<ItemTierValue> TierValues { get; private set; } = new List<ItemTierValue>();
    public ICollection<ItemEnchantment> Enchantments { get; private set; } = new List<ItemEnchantment>();

    private Item() { }

    public Item(string name, ItemSize size, Rarity minimumRarity, decimal? cooldown = null,
        string? cost = null, string? value = null, string? description = null, string? bazaarDbId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Item name cannot be empty.", nameof(name));

        Name = name.Trim();
        Size = size;
        MinimumRarity = minimumRarity;
        Cooldown = cooldown;
        Cost = cost?.Trim();
        Value = value?.Trim();
        Description = description?.Trim();
        BazaarDbId = bazaarDbId?.Trim();
    }

    public bool IsAvailableForHero(string heroName)
    {
        return Heroes.Any(h => h.Name == heroName || h.Name == "Neutral");
    }
}

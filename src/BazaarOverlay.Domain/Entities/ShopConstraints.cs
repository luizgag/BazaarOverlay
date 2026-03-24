using System.ComponentModel.DataAnnotations;
using BazaarOverlay.Domain.Enums;

namespace BazaarOverlay.Domain.Entities;

public class ShopConstraints
{
    [Key]
    public int Id { get; private set; }

    public int EncounterId { get; private set; }
    public Encounter Encounter { get; private set; } = null!;

    public ItemSize? MaxSize { get; private set; }
    public Rarity? MaxRarity { get; private set; }

    [MaxLength(50)]
    public string? RequiredEnchantment { get; private set; }

    public bool HeroOnly { get; private set; }

    private ShopConstraints() { }

    public ShopConstraints(ItemSize? maxSize, Rarity? maxRarity, string? requiredEnchantment, bool heroOnly)
    {
        MaxSize = maxSize;
        MaxRarity = maxRarity;
        RequiredEnchantment = requiredEnchantment?.Trim();
        HeroOnly = heroOnly;
    }
}

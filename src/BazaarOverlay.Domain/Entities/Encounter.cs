using System.ComponentModel.DataAnnotations;
using BazaarOverlay.Domain.Enums;

namespace BazaarOverlay.Domain.Entities;

public class Encounter
{
    [Key]
    public int Id { get; private set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; private set; } = string.Empty;

    [Required]
    public Rarity Rarity { get; private set; }

    [MaxLength(500)]
    public string Description { get; private set; } = string.Empty;

    [Required]
    public EncounterType Type { get; private set; }

    public bool IsHeroSpecific { get; private set; }

    public ShopConstraints? ShopConstraints { get; private set; }
    public ICollection<ShopAllowedTag> ShopAllowedTags { get; private set; } = new List<ShopAllowedTag>();
    public ICollection<Item> RewardItems { get; private set; } = new List<Item>();
    public ICollection<Skill> RewardSkills { get; private set; } = new List<Skill>();

    private Encounter() { }

    public Encounter(string name, Rarity rarity, string description, EncounterType type, bool isHeroSpecific)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Encounter name cannot be empty.", nameof(name));

        Name = name.Trim();
        Rarity = rarity;
        Description = description?.Trim() ?? string.Empty;
        Type = type;
        IsHeroSpecific = isHeroSpecific;
    }

    public void SetShopConstraints(ShopConstraints constraints)
    {
        if (Type != EncounterType.Shop)
            throw new InvalidOperationException("Shop constraints can only be set on shop encounters.");

        ShopConstraints = constraints;
    }
}

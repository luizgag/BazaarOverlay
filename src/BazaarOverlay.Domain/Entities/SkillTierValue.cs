using System.ComponentModel.DataAnnotations;
using BazaarOverlay.Domain.Enums;

namespace BazaarOverlay.Domain.Entities;

public class SkillTierValue
{
    [Key]
    public int Id { get; private set; }

    [Required]
    public Rarity Rarity { get; private set; }

    [Required]
    [MaxLength(500)]
    public string EffectDescription { get; private set; } = string.Empty;

    public int SkillId { get; private set; }
    public Skill Skill { get; private set; } = null!;

    private SkillTierValue() { }

    public SkillTierValue(Rarity rarity, string effectDescription)
    {
        if (string.IsNullOrWhiteSpace(effectDescription))
            throw new ArgumentException("Effect description cannot be empty.", nameof(effectDescription));

        Rarity = rarity;
        EffectDescription = effectDescription.Trim();
    }
}

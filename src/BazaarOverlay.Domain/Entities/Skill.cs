using System.ComponentModel.DataAnnotations;
using BazaarOverlay.Domain.Enums;

namespace BazaarOverlay.Domain.Entities;

public class Skill
{
    [Key]
    public int Id { get; private set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; private set; } = string.Empty;

    [Required]
    public Rarity MinimumRarity { get; private set; }

    public ICollection<SkillTag> Tags { get; private set; } = new List<SkillTag>();
    public ICollection<Hero> Heroes { get; private set; } = new List<Hero>();
    public ICollection<SkillTierValue> TierValues { get; private set; } = new List<SkillTierValue>();

    private Skill() { }

    public Skill(string name, Rarity minimumRarity)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Skill name cannot be empty.", nameof(name));

        Name = name.Trim();
        MinimumRarity = minimumRarity;
    }
}

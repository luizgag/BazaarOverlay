using System.ComponentModel.DataAnnotations;
using BazaarOverlay.Domain.Enums;

namespace BazaarOverlay.Domain.Entities;

public class EventOption
{
    [Key]
    public int Id { get; private set; }

    public int EventId { get; private set; }
    public Event Event { get; private set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Name { get; private set; } = string.Empty;

    [Required]
    public Rarity Tier { get; private set; }

    [MaxLength(500)]
    public string? Description { get; private set; }

    [MaxLength(50)]
    public string? HeroRestriction { get; private set; }

    private EventOption() { }

    public EventOption(string name, Rarity tier, string? description = null,
        string? heroRestriction = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Option name cannot be empty.", nameof(name));

        Name = name.Trim();
        Tier = tier;
        Description = description?.Trim();
        HeroRestriction = heroRestriction?.Trim();
    }
}

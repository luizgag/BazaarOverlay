using System.ComponentModel.DataAnnotations;
using BazaarOverlay.Domain.Enums;

namespace BazaarOverlay.Domain.Entities;

public class Monster
{
    [Key]
    public int Id { get; private set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; private set; } = string.Empty;

    [Required]
    public Rarity Tier { get; private set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int Level { get; private set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int Day { get; private set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int Health { get; private set; }

    [Range(0, int.MaxValue)]
    public int GoldReward { get; private set; }

    [Range(0, int.MaxValue)]
    public int XpReward { get; private set; }

    [MaxLength(50)]
    public string? BazaarDbId { get; private set; }

    public ICollection<Item> BoardItems { get; private set; } = new List<Item>();
    public ICollection<Skill> BoardSkills { get; private set; } = new List<Skill>();

    private Monster() { }

    public Monster(string name, Rarity tier, int level, int day, int health,
        int goldReward, int xpReward, string? bazaarDbId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Monster name cannot be empty.", nameof(name));
        if (health <= 0)
            throw new ArgumentException("Health must be positive.", nameof(health));
        if (day <= 0)
            throw new ArgumentException("Day must be positive.", nameof(day));

        Name = name.Trim();
        Tier = tier;
        Level = level;
        Day = day;
        Health = health;
        GoldReward = goldReward;
        XpReward = xpReward;
        BazaarDbId = bazaarDbId?.Trim();
    }
}

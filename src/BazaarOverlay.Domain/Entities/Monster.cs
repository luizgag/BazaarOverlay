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
    [Range(1, int.MaxValue)]
    public int Health { get; private set; }

    [Required]
    public Rarity Rarity { get; private set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int FirstDay { get; private set; }

    public ICollection<Item> DropItems { get; private set; } = new List<Item>();
    public ICollection<Skill> DropSkills { get; private set; } = new List<Skill>();

    private Monster() { }

    public Monster(string name, int health, Rarity rarity, int firstDay)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Monster name cannot be empty.", nameof(name));
        if (health <= 0)
            throw new ArgumentException("Health must be positive.", nameof(health));
        if (firstDay <= 0)
            throw new ArgumentException("First day must be positive.", nameof(firstDay));

        Name = name.Trim();
        Health = health;
        Rarity = rarity;
        FirstDay = firstDay;
    }

    public bool AppearsOnDay(int day) => day >= FirstDay;

    public IEnumerable<Item> GetDropsForHero(string heroName)
    {
        return DropItems.Where(i => i.IsAvailableForHero(heroName));
    }
}

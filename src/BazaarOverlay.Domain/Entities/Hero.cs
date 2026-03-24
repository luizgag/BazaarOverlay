using System.ComponentModel.DataAnnotations;

namespace BazaarOverlay.Domain.Entities;

public class Hero
{
    [Key]
    [Required]
    [MaxLength(50)]
    public string Name { get; private set; } = string.Empty;

    public ICollection<Item> Items { get; private set; } = new List<Item>();
    public ICollection<Skill> Skills { get; private set; } = new List<Skill>();

    private Hero() { }

    public Hero(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Hero name cannot be empty.", nameof(name));

        Name = name.Trim();
    }
}

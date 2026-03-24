using System.ComponentModel.DataAnnotations;

namespace BazaarOverlay.Domain.Entities;

public class Enchantment
{
    [Key]
    public int Id { get; private set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; private set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string GlobalDescription { get; private set; } = string.Empty;

    private Enchantment() { }

    public Enchantment(string name, string globalDescription)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Enchantment name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(globalDescription))
            throw new ArgumentException("Global description cannot be empty.", nameof(globalDescription));

        Name = name.Trim();
        GlobalDescription = globalDescription.Trim();
    }
}

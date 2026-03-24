using System.ComponentModel.DataAnnotations;

namespace BazaarOverlay.Domain.Entities;

public class ShopAllowedTag
{
    [Key]
    public int Id { get; private set; }

    [Required]
    [MaxLength(50)]
    public string Tag { get; private set; } = string.Empty;

    public int EncounterId { get; private set; }
    public Encounter Encounter { get; private set; } = null!;

    private ShopAllowedTag() { }

    public ShopAllowedTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Tag cannot be empty.", nameof(tag));

        Tag = tag.Trim();
    }
}

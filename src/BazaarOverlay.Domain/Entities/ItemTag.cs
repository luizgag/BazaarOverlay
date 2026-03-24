using System.ComponentModel.DataAnnotations;

namespace BazaarOverlay.Domain.Entities;

public class ItemTag
{
    [Key]
    public int Id { get; private set; }

    [Required]
    [MaxLength(50)]
    public string Tag { get; private set; } = string.Empty;

    public int ItemId { get; private set; }
    public Item Item { get; private set; } = null!;

    private ItemTag() { }

    public ItemTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Tag cannot be empty.", nameof(tag));

        Tag = tag.Trim();
    }
}

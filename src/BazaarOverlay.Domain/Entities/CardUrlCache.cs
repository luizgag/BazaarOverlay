using System.ComponentModel.DataAnnotations;

namespace BazaarOverlay.Domain.Entities;

public class CardUrlCache
{
    [Key]
    public int Id { get; private set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; private set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string CardUrl { get; private set; } = string.Empty;

    [MaxLength(50)]
    public string? Category { get; private set; }

    public DateTime CachedAt { get; private set; }

    private CardUrlCache() { }

    public CardUrlCache(string name, string cardUrl, string? category = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Cache entry name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(cardUrl))
            throw new ArgumentException("Card URL cannot be empty.", nameof(cardUrl));

        Name = name.Trim();
        CardUrl = cardUrl.Trim();
        Category = category?.Trim();
        CachedAt = DateTime.UtcNow;
    }
}

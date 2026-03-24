using System.ComponentModel.DataAnnotations;

namespace BazaarOverlay.Domain.Entities;

public class ItemEnchantment
{
    [Key]
    public int Id { get; private set; }

    public int ItemId { get; private set; }
    public Item Item { get; private set; } = null!;

    public int EnchantmentId { get; private set; }
    public Enchantment Enchantment { get; private set; } = null!;

    [Required]
    [MaxLength(500)]
    public string EffectDescription { get; private set; } = string.Empty;

    private ItemEnchantment() { }

    public ItemEnchantment(Enchantment enchantment, string effectDescription)
    {
        if (string.IsNullOrWhiteSpace(effectDescription))
            throw new ArgumentException("Effect description cannot be empty.", nameof(effectDescription));

        Enchantment = enchantment ?? throw new ArgumentNullException(nameof(enchantment));
        EnchantmentId = enchantment.Id;
        EffectDescription = effectDescription.Trim();
    }
}

using BazaarOverlay.Domain.Enums;

namespace BazaarOverlay.Application.DTOs;

public record ItemInfoResult(
    string Name,
    ItemSize Size,
    decimal? Cooldown,
    Rarity MinimumRarity,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Heroes,
    IReadOnlyList<TierValueResult> TierValues,
    IReadOnlyList<EnchantmentResult> Enchantments
);

public record TierValueResult(Rarity Rarity, string EffectDescription);

public record EnchantmentResult(string EnchantmentName, string EffectDescription);

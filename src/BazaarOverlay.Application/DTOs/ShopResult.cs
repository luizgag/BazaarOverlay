using BazaarOverlay.Domain.Enums;

namespace BazaarOverlay.Application.DTOs;

public record ShopResult(
    string ShopName,
    string Description,
    IReadOnlyList<ShopItemResult> AvailableItems
);

public record ShopItemResult(
    string Name,
    ItemSize Size,
    Rarity MinimumRarity,
    IReadOnlyList<string> Tags
);

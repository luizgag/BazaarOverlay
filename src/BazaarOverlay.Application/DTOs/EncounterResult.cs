using BazaarOverlay.Domain.Enums;

namespace BazaarOverlay.Application.DTOs;

public record EncounterResult(
    string Name,
    EncounterType Type,
    Rarity Rarity,
    string Description,
    bool IsHeroSpecific,
    MonsterEncounterResult? MonsterDetails,
    ShopResult? ShopDetails,
    EventEncounterResult? EventDetails
);

public record EventEncounterResult(
    string Name,
    string Description,
    IReadOnlyList<DropResult> RewardItems,
    IReadOnlyList<DropResult> RewardSkills
);

using BazaarOverlay.Domain.Enums;

namespace BazaarOverlay.Application.DTOs;

public record MonsterEncounterResult(
    string MonsterName,
    int Health,
    Rarity Rarity,
    int FirstDay,
    IReadOnlyList<DropResult> ItemDrops,
    IReadOnlyList<DropResult> SkillDrops
);

public record DropResult(
    string Name,
    Rarity MinimumRarity
);

using BazaarOverlay.Domain.Enums;

namespace BazaarOverlay.Application.DTOs;

public record SkillInfoResult(
    string Name,
    Rarity MinimumRarity,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Heroes,
    IReadOnlyList<TierValueResult> TierValues
);

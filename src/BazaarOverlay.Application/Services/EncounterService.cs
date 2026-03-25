using BazaarOverlay.Application.DTOs;
using BazaarOverlay.Application.Interfaces;
using BazaarOverlay.Domain.Enums;
using BazaarOverlay.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace BazaarOverlay.Application.Services;

public class EncounterService : IEncounterService
{
    private readonly IMonsterEncounterService _monsterService;
    private readonly IShopService _shopService;
    private readonly IEncounterRepository _encounterRepository;
    private readonly IRarityDayProbabilityRepository _rarityRepository;
    private readonly ILogger<EncounterService> _logger;

    public EncounterService(
        IMonsterEncounterService monsterService,
        IShopService shopService,
        IEncounterRepository encounterRepository,
        IRarityDayProbabilityRepository rarityRepository,
        ILogger<EncounterService> logger)
    {
        _monsterService = monsterService;
        _shopService = shopService;
        _encounterRepository = encounterRepository;
        _rarityRepository = rarityRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<EncounterResult>> SearchEncountersAsync(string partialName, string heroName, int currentDay)
    {
        _logger.LogInformation("Searching encounters: {Query} for {Hero} on day {Day}", partialName, heroName, currentDay);
        var encounters = await _encounterRepository.SearchByNameAsync(partialName);
        var availableRarities = await _rarityRepository.GetAvailableRaritiesForDayAsync(currentDay);
        var results = new List<EncounterResult>();

        foreach (var encounter in encounters)
        {
            EncounterResult result = encounter.Type switch
            {
                EncounterType.Monster => await BuildMonsterResult(encounter, heroName, currentDay),
                EncounterType.Shop => await BuildShopResult(encounter, heroName, currentDay),
                EncounterType.Event => BuildEventResult(encounter, heroName, availableRarities),
                _ => BuildBaseResult(encounter)
            };
            results.Add(result);
        }

        return results;
    }

    private async Task<EncounterResult> BuildMonsterResult(Domain.Entities.Encounter encounter, string heroName, int currentDay)
    {
        var monsterDetails = await _monsterService.GetMonsterEncounterAsync(encounter.Name, heroName, currentDay);
        return new EncounterResult(
            encounter.Name, encounter.Type, encounter.Rarity, encounter.Description,
            encounter.IsHeroSpecific, monsterDetails, null, null);
    }

    private async Task<EncounterResult> BuildShopResult(Domain.Entities.Encounter encounter, string heroName, int currentDay)
    {
        var shopDetails = await _shopService.GetShopItemsAsync(encounter.Name, heroName, currentDay);
        return new EncounterResult(
            encounter.Name, encounter.Type, encounter.Rarity, encounter.Description,
            encounter.IsHeroSpecific, null, shopDetails, null);
    }

    private static EncounterResult BuildEventResult(
        Domain.Entities.Encounter encounter, string heroName, IReadOnlyList<Rarity> availableRarities)
    {
        var itemDrops = encounter.RewardItems
            .Where(i => (!encounter.IsHeroSpecific || i.IsAvailableForHero(heroName))
                        && availableRarities.Contains(i.MinimumRarity))
            .Select(i => new DropResult(i.Name, i.MinimumRarity))
            .ToList();

        var skillDrops = encounter.RewardSkills
            .Where(s => (!encounter.IsHeroSpecific || s.Heroes.Any(h => h.Name == heroName || h.Name == "Neutral"))
                        && availableRarities.Contains(s.MinimumRarity))
            .Select(s => new DropResult(s.Name, s.MinimumRarity))
            .ToList();

        var eventDetails = new EventEncounterResult(encounter.Name, encounter.Description, itemDrops, skillDrops);

        return new EncounterResult(
            encounter.Name, encounter.Type, encounter.Rarity, encounter.Description,
            encounter.IsHeroSpecific, null, null, eventDetails);
    }

    private static EncounterResult BuildBaseResult(Domain.Entities.Encounter encounter)
    {
        return new EncounterResult(
            encounter.Name, encounter.Type, encounter.Rarity, encounter.Description,
            encounter.IsHeroSpecific, null, null, null);
    }
}

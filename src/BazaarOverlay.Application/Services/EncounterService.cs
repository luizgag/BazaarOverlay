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
    private readonly IMonsterRepository _monsterRepository;
    private readonly ILogger<EncounterService> _logger;

    public EncounterService(
        IMonsterEncounterService monsterService,
        IShopService shopService,
        IEncounterRepository encounterRepository,
        IRarityDayProbabilityRepository rarityRepository,
        IMonsterRepository monsterRepository,
        ILogger<EncounterService> logger)
    {
        _monsterService = monsterService;
        _shopService = shopService;
        _encounterRepository = encounterRepository;
        _rarityRepository = rarityRepository;
        _monsterRepository = monsterRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<EncounterResult>> SearchEncountersAsync(string partialName, string? heroName, int currentDay)
    {
        _logger.LogInformation("Searching encounters: {Query} for {Hero} on day {Day}", partialName, heroName ?? "(none)", currentDay);
        var encounters = await _encounterRepository.SearchByNameAsync(partialName);
        var availableRarities = await _rarityRepository.GetAvailableRaritiesForDayAsync(currentDay);
        var results = new List<EncounterResult>();
        var encounteredNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var encounter in encounters)
        {
            encounteredNames.Add(encounter.Name);
            EncounterResult result = encounter.Type switch
            {
                EncounterType.Monster => await BuildMonsterResult(encounter, heroName, currentDay),
                EncounterType.Shop => await BuildShopResult(encounter, heroName, currentDay),
                EncounterType.Event => BuildEventResult(encounter, heroName, availableRarities),
                _ => BuildBaseResult(encounter)
            };
            results.Add(result);
        }

        var monsters = await _monsterRepository.SearchByNameAsync(partialName);
        foreach (var monster in monsters)
        {
            if (encounteredNames.Contains(monster.Name))
                continue;

            results.Add(BuildMonsterResultFromEntity(monster, heroName));
        }

        return results;
    }

    private async Task<EncounterResult> BuildMonsterResult(Domain.Entities.Encounter encounter, string? heroName, int currentDay)
    {
        MonsterEncounterResult? monsterDetails;
        if (heroName is not null)
        {
            monsterDetails = await _monsterService.GetMonsterEncounterAsync(encounter.Name, heroName, currentDay);
        }
        else
        {
            var monster = await _monsterRepository.GetByNameAsync(encounter.Name);
            monsterDetails = monster is not null
                ? BuildMonsterDetails(monster, null)
                : null;
        }

        return new EncounterResult(
            encounter.Name, encounter.Type, encounter.Rarity, encounter.Description,
            encounter.IsHeroSpecific, monsterDetails, null, null);
    }

    private static EncounterResult BuildMonsterResultFromEntity(Domain.Entities.Monster monster, string? heroName)
    {
        var monsterDetails = BuildMonsterDetails(monster, heroName);

        return new EncounterResult(
            monster.Name, EncounterType.Monster, monster.Rarity, string.Empty,
            false, monsterDetails, null, null);
    }

    private static MonsterEncounterResult BuildMonsterDetails(Domain.Entities.Monster monster, string? heroName)
    {
        var itemDrops = monster.DropItems
            .Where(i => heroName is null || i.IsAvailableForHero(heroName))
            .Select(i => new DropResult(i.Name, i.MinimumRarity))
            .ToList();

        var skillDrops = monster.DropSkills
            .Where(s => heroName is null || s.Heroes.Any(h => h.Name == heroName || h.Name == "Neutral"))
            .Select(s => new DropResult(s.Name, s.MinimumRarity))
            .ToList();

        return new MonsterEncounterResult(monster.Name, monster.Health, monster.Rarity, monster.FirstDay, itemDrops, skillDrops);
    }

    private async Task<EncounterResult> BuildShopResult(Domain.Entities.Encounter encounter, string? heroName, int currentDay)
    {
        ShopResult? shopDetails = null;
        if (heroName is not null)
            shopDetails = await _shopService.GetShopItemsAsync(encounter.Name, heroName, currentDay);

        return new EncounterResult(
            encounter.Name, encounter.Type, encounter.Rarity, encounter.Description,
            encounter.IsHeroSpecific, null, shopDetails, null);
    }

    private static EncounterResult BuildEventResult(
        Domain.Entities.Encounter encounter, string? heroName, IReadOnlyList<Rarity> availableRarities)
    {
        var itemDrops = encounter.RewardItems
            .Where(i => (heroName is null || !encounter.IsHeroSpecific || i.IsAvailableForHero(heroName))
                        && availableRarities.Contains(i.MinimumRarity))
            .Select(i => new DropResult(i.Name, i.MinimumRarity))
            .ToList();

        var skillDrops = encounter.RewardSkills
            .Where(s => (heroName is null || !encounter.IsHeroSpecific || s.Heroes.Any(h => h.Name == heroName || h.Name == "Neutral"))
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

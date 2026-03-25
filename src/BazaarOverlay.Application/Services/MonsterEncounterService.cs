using BazaarOverlay.Application.DTOs;
using BazaarOverlay.Application.Interfaces;
using BazaarOverlay.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace BazaarOverlay.Application.Services;

public class MonsterEncounterService : IMonsterEncounterService
{
    private readonly IMonsterRepository _monsterRepository;
    private readonly IRarityDayProbabilityRepository _rarityRepository;
    private readonly ILogger<MonsterEncounterService> _logger;

    public MonsterEncounterService(
        IMonsterRepository monsterRepository,
        IRarityDayProbabilityRepository rarityRepository,
        ILogger<MonsterEncounterService> logger)
    {
        _monsterRepository = monsterRepository;
        _rarityRepository = rarityRepository;
        _logger = logger;
    }

    public async Task<MonsterEncounterResult?> GetMonsterEncounterAsync(string monsterName, string heroName, int currentDay)
    {
        _logger.LogInformation("Looking up monster encounter: {MonsterName} for {Hero} on day {Day}", monsterName, heroName, currentDay);
        var monster = await _monsterRepository.GetByNameAsync(monsterName);
        if (monster is null)
        {
            _logger.LogWarning("Monster not found: {MonsterName}", monsterName);
            return null;
        }

        var availableRarities = await _rarityRepository.GetAvailableRaritiesForDayAsync(currentDay);

        var itemDrops = monster.DropItems
            .Where(i => i.IsAvailableForHero(heroName) && availableRarities.Contains(i.MinimumRarity))
            .Select(i => new DropResult(i.Name, i.MinimumRarity))
            .ToList();

        var skillDrops = monster.DropSkills
            .Where(s => s.Heroes.Any(h => h.Name == heroName || h.Name == "Neutral")
                        && availableRarities.Contains(s.MinimumRarity))
            .Select(s => new DropResult(s.Name, s.MinimumRarity))
            .ToList();

        return new MonsterEncounterResult(
            monster.Name,
            monster.Health,
            monster.Rarity,
            monster.FirstDay,
            itemDrops,
            skillDrops
        );
    }

    public async Task<IReadOnlyList<MonsterEncounterResult>> SearchMonstersAsync(string partialName, string heroName, int currentDay)
    {
        _logger.LogInformation("Searching monsters: {Query} for {Hero} on day {Day}", partialName, heroName, currentDay);
        var monsters = await _monsterRepository.SearchByNameAsync(partialName);
        var availableRarities = await _rarityRepository.GetAvailableRaritiesForDayAsync(currentDay);
        var results = new List<MonsterEncounterResult>();

        foreach (var monster in monsters)
        {
            var itemDrops = monster.DropItems
                .Where(i => i.IsAvailableForHero(heroName) && availableRarities.Contains(i.MinimumRarity))
                .Select(i => new DropResult(i.Name, i.MinimumRarity))
                .ToList();

            var skillDrops = monster.DropSkills
                .Where(s => s.Heroes.Any(h => h.Name == heroName || h.Name == "Neutral")
                            && availableRarities.Contains(s.MinimumRarity))
                .Select(s => new DropResult(s.Name, s.MinimumRarity))
                .ToList();

            results.Add(new MonsterEncounterResult(
                monster.Name,
                monster.Health,
                monster.Rarity,
                monster.FirstDay,
                itemDrops,
                skillDrops
            ));
        }

        return results;
    }
}

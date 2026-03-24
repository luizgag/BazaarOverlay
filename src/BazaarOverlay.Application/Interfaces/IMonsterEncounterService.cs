using BazaarOverlay.Application.DTOs;

namespace BazaarOverlay.Application.Interfaces;

public interface IMonsterEncounterService
{
    Task<MonsterEncounterResult?> GetMonsterEncounterAsync(string monsterName, string heroName, int currentDay);
    Task<IReadOnlyList<MonsterEncounterResult>> SearchMonstersAsync(string partialName, string heroName, int currentDay);
}

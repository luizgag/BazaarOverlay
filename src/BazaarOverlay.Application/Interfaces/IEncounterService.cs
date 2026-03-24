using BazaarOverlay.Application.DTOs;

namespace BazaarOverlay.Application.Interfaces;

public interface IEncounterService
{
    Task<IReadOnlyList<EncounterResult>> SearchEncountersAsync(string partialName, string heroName, int currentDay);
}

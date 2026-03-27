using BazaarOverlay.Application.DTOs;

namespace BazaarOverlay.Application.Interfaces;

public interface IEncounterService
{
    Task<IReadOnlyList<EncounterResult>> SearchEncountersAsync(string query, string? hero, int day);
}

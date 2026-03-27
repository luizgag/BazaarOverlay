using BazaarOverlay.Application.DTOs;

namespace BazaarOverlay.Application.Services;

public class EncounterService : Interfaces.IEncounterService
{
    public Task<IReadOnlyList<EncounterResult>> SearchEncountersAsync(string query, string? hero, int day)
        => Task.FromResult<IReadOnlyList<EncounterResult>>(Array.Empty<EncounterResult>());
}

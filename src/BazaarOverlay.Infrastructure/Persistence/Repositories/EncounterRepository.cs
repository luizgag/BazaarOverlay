using BazaarOverlay.Domain.Interfaces;

namespace BazaarOverlay.Infrastructure.Persistence.Repositories;

public class EncounterRepository : IEncounterRepository
{
    public EncounterRepository(BazaarDbContext context) { }
}

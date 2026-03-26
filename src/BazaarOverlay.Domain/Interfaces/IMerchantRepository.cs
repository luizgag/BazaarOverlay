using BazaarOverlay.Domain.Entities;

namespace BazaarOverlay.Domain.Interfaces;

public interface IMerchantRepository
{
    Task<Merchant?> GetByNameAsync(string name);
    Task<IReadOnlyList<Merchant>> SearchByNameAsync(string partialName);
    Task AddAsync(Merchant merchant);
}

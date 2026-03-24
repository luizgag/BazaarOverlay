using BazaarOverlay.Application.DTOs;

namespace BazaarOverlay.Application.Interfaces;

public interface IItemInfoService
{
    Task<ItemInfoResult?> GetItemInfoAsync(string itemName);
    Task<IReadOnlyList<ItemInfoResult>> SearchItemsAsync(string partialName);
}

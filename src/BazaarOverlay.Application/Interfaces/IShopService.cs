using BazaarOverlay.Application.DTOs;

namespace BazaarOverlay.Application.Interfaces;

public interface IShopService
{
    Task<ShopResult?> GetShopItemsAsync(string shopName, string heroName, int currentDay);
    Task<IReadOnlyList<ShopResult>> SearchShopsAsync(string partialName, string heroName, int currentDay);
}

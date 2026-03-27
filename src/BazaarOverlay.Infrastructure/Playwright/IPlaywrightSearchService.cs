namespace BazaarOverlay.Infrastructure.Playwright;

public interface IPlaywrightSearchService
{
    /// <summary>
    /// Searches bazaardb.gg for the given name and returns the card URL path (e.g., "/card/123/pigomorph"), or null if not found.
    /// </summary>
    Task<(string? CardUrl, string? Category)> SearchAsync(string name);
}

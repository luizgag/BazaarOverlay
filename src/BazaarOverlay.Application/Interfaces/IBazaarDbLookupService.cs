namespace BazaarOverlay.Application.Interfaces;

public interface IBazaarDbLookupService
{
    /// <summary>
    /// Looks up a card URL on bazaardb.gg by entity name. Uses cache first, then Playwright search.
    /// Returns the full card page URL, or null if not found.
    /// </summary>
    Task<string?> GetCardUrlAsync(string name);
}

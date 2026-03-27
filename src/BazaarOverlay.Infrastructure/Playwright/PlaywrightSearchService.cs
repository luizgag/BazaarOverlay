using System.Web;
using Microsoft.Extensions.Logging;

namespace BazaarOverlay.Infrastructure.Playwright;

public class PlaywrightSearchService : IPlaywrightSearchService
{
    private readonly IPlaywrightBrowserManager _browserManager;
    private readonly ILogger<PlaywrightSearchService> _logger;

    public PlaywrightSearchService(
        IPlaywrightBrowserManager browserManager,
        ILogger<PlaywrightSearchService> logger)
    {
        _browserManager = browserManager;
        _logger = logger;
    }

    public async Task<(string? CardUrl, string? Category)> SearchAsync(string name)
    {
        var context = await _browserManager.GetBrowserContextAsync();
        var page = await context.NewPageAsync();

        try
        {
            var encodedName = HttpUtility.UrlEncode(name);
            var searchUrl = $"https://bazaardb.gg/search?q={encodedName}";
            _logger.LogInformation("Searching bazaardb.gg for: {Name}", name);

            await page.GotoAsync(searchUrl, new() { WaitUntil = Microsoft.Playwright.WaitUntilState.NetworkIdle });

            // Wait for search results to render
            var firstResult = await page.QuerySelectorAsync("a[href^='/items/'], a[href^='/skills/'], a[href^='/monsters/'], a[href^='/encounters/']");
            if (firstResult is null)
            {
                _logger.LogWarning("No search results found for: {Name}", name);
                return (null, null);
            }

            var href = await firstResult.GetAttributeAsync("href");
            if (string.IsNullOrEmpty(href))
                return (null, null);

            // Determine category from URL path
            var category = href.Split('/').ElementAtOrDefault(1);

            var fullUrl = $"https://bazaardb.gg{href}";
            _logger.LogInformation("Found card URL: {Url} (category: {Category})", fullUrl, category);
            return (fullUrl, category);
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}

using Microsoft.Playwright;

namespace BazaarOverlay.Infrastructure.Playwright;

public interface IPlaywrightBrowserManager : IAsyncDisposable
{
    Task<IBrowserContext> GetBrowserContextAsync();
}

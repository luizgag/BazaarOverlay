using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace BazaarOverlay.Infrastructure.Playwright;

public class PlaywrightBrowserManager : IPlaywrightBrowserManager
{
    private readonly ILogger<PlaywrightBrowserManager> _logger;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public PlaywrightBrowserManager(ILogger<PlaywrightBrowserManager> logger)
    {
        _logger = logger;
    }

    public async Task<IBrowserContext> GetBrowserContextAsync()
    {
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_context is not null)
                return _context;

            _logger.LogInformation("Launching headless Playwright Chromium...");
            _playwright = await Microsoft.Playwright.Playwright.CreateAsync().ConfigureAwait(false);
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            }).ConfigureAwait(false);
            _context = await _browser.NewContextAsync().ConfigureAwait(false);
            _logger.LogInformation("Playwright browser context ready");
            return _context;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_context is not null) await _context.DisposeAsync().ConfigureAwait(false);
        if (_browser is not null) await _browser.DisposeAsync().ConfigureAwait(false);
        _playwright?.Dispose();
        _lock.Dispose();
        GC.SuppressFinalize(this);
    }
}

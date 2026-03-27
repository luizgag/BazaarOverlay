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
        await _lock.WaitAsync();
        try
        {
            if (_context is not null)
                return _context;

            _logger.LogInformation("Launching headless Playwright Chromium...");
            _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });
            _context = await _browser.NewContextAsync();
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
        if (_context is not null) await _context.DisposeAsync();
        if (_browser is not null) await _browser.DisposeAsync();
        _playwright?.Dispose();
        _lock.Dispose();
        GC.SuppressFinalize(this);
    }
}

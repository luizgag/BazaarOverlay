using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.Extensions.Logging;
using BazaarOverlay.Application.Interfaces;
using BazaarOverlay.Application.Services;
using BazaarOverlay.Application.ViewModels;
using BazaarOverlay.Domain.Interfaces;
using BazaarOverlay.Infrastructure.DataImport;
using BazaarOverlay.Infrastructure.Ocr;
using BazaarOverlay.Infrastructure.Persistence;
using BazaarOverlay.Infrastructure.Persistence.Repositories;
using BazaarOverlay.Infrastructure.Playwright;
using BazaarOverlay.Infrastructure.ScreenCapture;
using BazaarOverlay.WPF.Services;
using BazaarOverlay.WPF.ViewModels;
using BazaarOverlay.WPF.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BazaarOverlay.WPF;

public partial class App : System.Windows.Application
{
#if DEBUG
    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AllocConsole();
#endif

    private ServiceProvider? _serviceProvider;
    private ILogger<App>? _logger;

    protected override void OnStartup(StartupEventArgs e)
    {
#if DEBUG
        AllocConsole();
#endif
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
        _logger = _serviceProvider.GetRequiredService<ILogger<App>>();
        _logger.LogInformation("BazaarOverlay starting up...");

        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<BazaarDbContext>();
            context.Database.EnsureCreated();
            _logger.LogInformation("Database created/verified");

            var importService = scope.ServiceProvider.GetRequiredService<IDataImportService>();
            importService.SeedHeroesAsync().GetAwaiter().GetResult();
            importService.SeedRarityProbabilitiesAsync().GetAwaiter().GetResult();
        }

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();

        // Set up global hotkeys
        var hotkeyService = _serviceProvider.GetRequiredService<HotkeyService>();
        var windowHandle = new System.Windows.Interop.WindowInteropHelper(mainWindow).Handle;
        hotkeyService.Register(windowHandle);

        // Ctrl+D: Capture and show card overlay
        hotkeyService.HotkeyPressed += async () =>
        {
            var orchestrator = _serviceProvider.GetRequiredService<IOverlayOrchestrator>();
            await orchestrator.HandleHotkeyAsync();
        };

        // Ctrl+H: Toggle menu overlay visibility
        hotkeyService.MenuHotkeyPressed += () =>
        {
            mainWindow.Visibility = mainWindow.Visibility == Visibility.Visible
                ? Visibility.Hidden
                : Visibility.Visible;
        };

        // Pre-create overlay window (hidden)
        _serviceProvider.GetRequiredService<CardOverlayWindow>();

        _logger.LogInformation("Overlay hotkeys registered (Ctrl+D: card, Ctrl+H: menu)");
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BazaarOverlay", "bazaar.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        services.AddDbContext<BazaarDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}",
                o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

        // Repositories
        services.AddScoped<IHeroRepository, HeroRepository>();
        services.AddScoped<IMonsterRepository, MonsterRepository>();
        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<ISkillRepository, SkillRepository>();
        services.AddScoped<IRarityDayProbabilityRepository, RarityDayProbabilityRepository>();
        services.AddScoped<IEncounterRepository, EncounterRepository>();
        services.AddScoped<ICardUrlCacheRepository, CardUrlCacheRepository>();

        // Application services
        services.AddSingleton<IGameSessionService, GameSessionService>();
        services.AddScoped<IMonsterEncounterService, MonsterEncounterService>();
        services.AddScoped<IItemInfoService, ItemInfoService>();
        services.AddScoped<ISkillInfoService, SkillInfoService>();
        services.AddScoped<IShopService, ShopService>();
        services.AddScoped<IEncounterService, EncounterService>();
        services.AddScoped<IDataImportService, DataImportService>();
        services.AddScoped<ITooltipNameExtractor, TooltipNameExtractor>();
        services.AddScoped<IScreenCaptureService, ScreenCaptureService>();
        services.AddScoped<IOcrService, WindowsOcrService>();
        services.AddScoped<IBazaarDbLookupService, BazaarDbLookupService>();
        services.AddScoped<IOverlayOrchestrator, OverlayOrchestrator>();
        services.AddHttpClient();
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
        });
        services.AddTransient<BazaarPlannerImporter>();

        // Playwright services (long-lived browser)
        services.AddSingleton<IPlaywrightBrowserManager, PlaywrightBrowserManager>();
        services.AddSingleton<IPlaywrightSearchService, PlaywrightSearchService>();

        // Overlay components
        services.AddSingleton<CardOverlayViewModel>();
        services.AddSingleton<CardOverlayWindow>();
        services.AddSingleton<HotkeyService>();

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<MonsterEncounterViewModel>();
        services.AddTransient<ItemSkillInfoViewModel>();

        // Windows
        services.AddTransient<MainWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _logger?.LogInformation("BazaarOverlay shutting down");
        if (_serviceProvider?.GetService<IPlaywrightBrowserManager>() is IAsyncDisposable browserManager)
            browserManager.DisposeAsync().AsTask().GetAwaiter().GetResult();
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}

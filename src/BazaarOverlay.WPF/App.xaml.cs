using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.Extensions.Logging;
using BazaarOverlay.Application.Interfaces;
using BazaarOverlay.Application.Services;
using BazaarOverlay.Domain.Interfaces;
using BazaarOverlay.Infrastructure.DataImport;
using BazaarOverlay.Infrastructure.Persistence;
using BazaarOverlay.Infrastructure.Persistence.Repositories;
using BazaarOverlay.WPF.ViewModels;
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
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "BazaarOverlay", "bazaar.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        services.AddDbContext<BazaarDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // Repositories
        services.AddScoped<IHeroRepository, HeroRepository>();
        services.AddScoped<IMonsterRepository, MonsterRepository>();
        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<ISkillRepository, SkillRepository>();
        services.AddScoped<IRarityDayProbabilityRepository, RarityDayProbabilityRepository>();
        services.AddScoped<IEncounterRepository, EncounterRepository>();

        // Application services
        services.AddSingleton<IGameSessionService, GameSessionService>();
        services.AddScoped<IMonsterEncounterService, MonsterEncounterService>();
        services.AddScoped<IItemInfoService, ItemInfoService>();
        services.AddScoped<ISkillInfoService, SkillInfoService>();
        services.AddScoped<IShopService, ShopService>();
        services.AddScoped<IEncounterService, EncounterService>();
        services.AddScoped<IDataImportService, DataImportService>();
        services.AddHttpClient();
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
        });
        services.AddTransient<BazaarPlannerImporter>();

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
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}

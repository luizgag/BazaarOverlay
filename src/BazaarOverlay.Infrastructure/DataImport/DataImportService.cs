using BazaarOverlay.Application.Interfaces;
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BazaarOverlay.Infrastructure.DataImport;

// TODO Task 15: Rewrite DataImportService to use BazaarDbScraper
public class DataImportService : IDataImportService
{
    private readonly BazaarDbContext _context;
    private readonly ILogger<DataImportService> _logger;

    public DataImportService(BazaarDbContext context, ILogger<DataImportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public Task ImportAllAsync(IProgress<string>? progress = null)
    {
        _logger.LogWarning("DataImportService.ImportAllAsync is not yet implemented (pending Task 15 rewrite)");
        return Task.CompletedTask;
    }

    public async Task SeedHeroesAsync()
    {
        _logger.LogInformation("Seeding heroes...");
        var heroNames = new[] { "Dooley", "Jules", "Mak", "Pygmalien", "Stelle", "Vanessa", "Neutral" };
        foreach (var name in heroNames)
        {
            if (!await _context.Heroes.AnyAsync(h => h.Name == name))
                _context.Heroes.Add(new Hero(name));
        }
        await _context.SaveChangesAsync();
        _logger.LogInformation("Heroes seeded successfully");
    }

    public Task SeedRarityProbabilitiesAsync()
    {
        _logger.LogWarning("DataImportService.SeedRarityProbabilitiesAsync is not yet implemented (pending Task 15 rewrite)");
        return Task.CompletedTask;
    }
}

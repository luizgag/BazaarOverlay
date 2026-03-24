namespace BazaarOverlay.Application.Interfaces;

public interface IDataImportService
{
    Task ImportAllAsync(IProgress<string>? progress = null);
    Task SeedRarityProbabilitiesAsync();
    Task SeedHeroesAsync();
}

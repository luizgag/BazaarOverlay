using BazaarOverlay.Infrastructure.DataImport;
using Shouldly;

namespace BazaarOverlay.Tests.Infrastructure;

public class DataImportTests
{
    [Fact]
    public void ParseJsArray_WithValidItemsJs_ReturnsItems()
    {
        var js = """
            export const items = [
                {"Name": "Rusty Sword", "Size": "Small", "StartingTier": "Bronze", "Tags": ["Weapon"], "Heroes": ["Vanessa"]},
                {"Name": "Shield", "Size": "Medium", "StartingTier": "Bronze", "Tags": ["Armor"], "Heroes": null}
            ];
            """;

        var result = BazaarPlannerImporter.ParseJsArray<BazaarPlannerItem>(js);

        result.Count.ShouldBe(2);
        result[0].Name.ShouldBe("Rusty Sword");
        result[0].Size.ShouldBe("Small");
        result[0].Tags.ShouldContain("Weapon");
        result[1].Name.ShouldBe("Shield");
    }

    [Fact]
    public void ParseJsArray_WithValidMonstersJs_ReturnsMonsters()
    {
        var js = """
            export const monsters = [
                {"Name": "Goblin", "Tier": "Bronze", "Health": 45, "Day": 1, "Items": ["Rusty Sword"], "Skills": ["Quick Strike"]}
            ];
            """;

        var result = BazaarPlannerImporter.ParseJsArray<BazaarPlannerMonster>(js);

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Goblin");
        result[0].Health.ShouldBe(45);
        result[0].Items.ShouldContain("Rusty Sword");
    }

    [Fact]
    public void ParseJsArray_WithTrailingCommas_HandlesGracefully()
    {
        var js = """
            export const items = [
                {"Name": "Sword", "Size": "Small",},
            ];
            """;

        var result = BazaarPlannerImporter.ParseJsArray<BazaarPlannerItem>(js);

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Sword");
    }

    [Fact]
    public void ParseJsArray_WithInvalidContent_ReturnsEmptyList()
    {
        var js = "this is not valid js";

        var result = BazaarPlannerImporter.ParseJsArray<BazaarPlannerItem>(js);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task DataImportService_SeedHeroes_CreatesAllHeroes()
    {
        using var context = TestDbContextFactory.Create();
        var httpClient = new HttpClient();
        var importer = new BazaarPlannerImporter(httpClient);
        var service = new DataImportService(context, importer);

        await service.SeedHeroesAsync();

        context.Heroes.Count().ShouldBe(7); // 6 heroes + Neutral
    }

    [Fact]
    public async Task DataImportService_SeedRarityProbabilities_PopulatesTable()
    {
        using var context = TestDbContextFactory.Create();
        var httpClient = new HttpClient();
        var importer = new BazaarPlannerImporter(httpClient);
        var service = new DataImportService(context, importer);

        await service.SeedRarityProbabilitiesAsync();

        context.RarityDayProbabilities.Count().ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task DataImportService_SeedHeroes_IsIdempotent()
    {
        using var context = TestDbContextFactory.Create();
        var httpClient = new HttpClient();
        var importer = new BazaarPlannerImporter(httpClient);
        var service = new DataImportService(context, importer);

        await service.SeedHeroesAsync();
        await service.SeedHeroesAsync(); // second call

        context.Heroes.Count().ShouldBe(7);
    }
}

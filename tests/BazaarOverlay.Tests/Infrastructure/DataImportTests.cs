using BazaarOverlay.Infrastructure.DataImport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace BazaarOverlay.Tests.Infrastructure;

public class DataImportTests
{
    [Fact]
    public void ParseJsExport_WithObjectFormat_ReturnsItems()
    {
        var js = """
            export const items = {
                "Rusty Sword": {"name": "Rusty Sword", "tier": 2, "tags": ["Weapon", "Small"], "cooldown": null, "ammo": null},
                "Shield": {"name": "Shield", "tier": 1, "tags": ["Armor", "Medium"], "cooldown": null, "ammo": null}
            };
            """;

        var result = BazaarPlannerImporter.ParseJsExport<BazaarPlannerItem>(js);

        result.Count.ShouldBe(2);
        result[0].Name.ShouldBe("Rusty Sword");
        result[0].Tags.ShouldContain("Weapon");
        result[1].Name.ShouldBe("Shield");
    }

    [Fact]
    public void ParseJsExport_WithObjectFormat_ReturnsMonsters()
    {
        var js = """
            export const monsters = {
                "Goblin": {"name": "Goblin", "day": 1, "health": 45, "skills": [{"name": "Bite", "tier": 0}], "items": [{"name": "Fang", "tier": 0}]}
            };
            """;

        var result = BazaarPlannerImporter.ParseJsExport<BazaarPlannerMonster>(js);

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Goblin");
        result[0].Health.ShouldBe(45);
        result[0].Day.ShouldBe(1);
    }

    [Fact]
    public void ParseJsExport_WithObjectFormat_ReturnsSkills()
    {
        var js = """
            export const skills = {
                "Above the Clouds": {"name": "Above the Clouds", "tier": 2, "tags": ["Stelle", "FlyingReference", "Crit"]}
            };
            """;

        var result = BazaarPlannerImporter.ParseJsExport<BazaarPlannerSkill>(js);

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Above the Clouds");
        result[0].Tags.ShouldContain("Stelle");
    }

    [Fact]
    public void ParseJsExport_WithArrayFormat_StillWorks()
    {
        var js = """
            export const items = [
                {"name": "Sword", "tags": ["Weapon"]}
            ];
            """;

        var result = BazaarPlannerImporter.ParseJsExport<BazaarPlannerItem>(js);

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Sword");
    }

    [Fact]
    public void ParseJsExport_WithTrailingCommas_HandlesGracefully()
    {
        var js = """
            export const items = {
                "Sword": {"name": "Sword", "tags": ["Weapon"],},
            };
            """;

        var result = BazaarPlannerImporter.ParseJsExport<BazaarPlannerItem>(js);

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Sword");
    }

    [Fact]
    public void ParseJsArray_WithInvalidContent_ReturnsEmptyList()
    {
        var js = "this is not valid js";

        var result = BazaarPlannerImporter.ParseJsExport<BazaarPlannerItem>(js);

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task DataImportService_SeedHeroes_CreatesAllHeroes()
    {
        using var context = TestDbContextFactory.Create();
        var httpClient = new HttpClient();
        var importer = new BazaarPlannerImporter(httpClient, NullLogger<BazaarPlannerImporter>.Instance);
        var service = new DataImportService(context, importer, NullLogger<DataImportService>.Instance);

        await service.SeedHeroesAsync();

        context.Heroes.Count().ShouldBe(7); // 6 heroes + Neutral
    }

    [Fact]
    public async Task DataImportService_SeedRarityProbabilities_PopulatesTable()
    {
        using var context = TestDbContextFactory.Create();
        var httpClient = new HttpClient();
        var importer = new BazaarPlannerImporter(httpClient, NullLogger<BazaarPlannerImporter>.Instance);
        var service = new DataImportService(context, importer, NullLogger<DataImportService>.Instance);

        await service.SeedRarityProbabilitiesAsync();

        context.RarityDayProbabilities.Count().ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task DataImportService_SeedHeroes_IsIdempotent()
    {
        using var context = TestDbContextFactory.Create();
        var httpClient = new HttpClient();
        var importer = new BazaarPlannerImporter(httpClient, NullLogger<BazaarPlannerImporter>.Instance);
        var service = new DataImportService(context, importer, NullLogger<DataImportService>.Instance);

        await service.SeedHeroesAsync();
        await service.SeedHeroesAsync(); // second call

        context.Heroes.Count().ShouldBe(7);
    }
}

using BazaarOverlay.Infrastructure.DataImport;
using BazaarOverlay.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace BazaarOverlay.Tests.Infrastructure;

public class DataImportTests
{
    // --- ParseJsExport logging tests ---

    [Fact]
    public void ParseJsExport_WhenJsonInvalid_LogsWarning()
    {
        var logger = new TestLogger<BazaarPlannerImporter>();
        // Matches array pattern but has invalid JSON inside
        var js = """export const items = [not valid json at all]""";

        var result = BazaarPlannerImporter.ParseJsExport<BazaarPlannerItem>(js, logger);

        result.ShouldBeEmpty();
        logger.HasEntry(LogLevel.Warning, "Failed to deserialize").ShouldBeTrue();
    }

    [Fact]
    public void ParseJsExport_WhenNoPatternMatches_LogsWarning()
    {
        var logger = new TestLogger<BazaarPlannerImporter>();
        var js = "this is not valid js export content";

        var result = BazaarPlannerImporter.ParseJsExport<BazaarPlannerItem>(js, logger);

        result.ShouldBeEmpty();
        logger.HasEntry(LogLevel.Warning, "No export pattern matched").ShouldBeTrue();
    }

    [Fact]
    public void ParseJsExport_WhenObjectJsonInvalid_LogsWarning()
    {
        var logger = new TestLogger<BazaarPlannerImporter>();
        // Matches object pattern but has invalid JSON
        var js = """export const items = {not valid json}""";

        var result = BazaarPlannerImporter.ParseJsExport<BazaarPlannerItem>(js, logger);

        result.ShouldBeEmpty();
        logger.HasEntry(LogLevel.Warning, "Failed to deserialize").ShouldBeTrue();
    }

    [Fact]
    public void ParseJsExport_WhenSuccessful_LogsMatchedFormat()
    {
        var logger = new TestLogger<BazaarPlannerImporter>();
        var js = """
            export const items = [
                {"name": "Sword", "tags": ["Weapon"]}
            ];
            """;

        var result = BazaarPlannerImporter.ParseJsExport<BazaarPlannerItem>(js, logger);

        result.Count.ShouldBe(1);
        logger.HasEntry(LogLevel.Debug, "array").ShouldBeTrue();
    }

    [Fact]
    public void ParseJsExport_WhenObjectFormatSuccessful_LogsMatchedFormat()
    {
        var logger = new TestLogger<BazaarPlannerImporter>();
        var js = """
            export const items = {
                "Sword": {"name": "Sword", "tags": ["Weapon"]}
            };
            """;

        var result = BazaarPlannerImporter.ParseJsExport<BazaarPlannerItem>(js, logger);

        result.Count.ShouldBe(1);
        logger.HasEntry(LogLevel.Debug, "object").ShouldBeTrue();
    }

    // --- Fetch method logging tests ---

    [Fact]
    public async Task FetchItemsAsync_LogsContentLength()
    {
        var js = """export const items = {"Sword": {"name": "Sword", "tier": 0, "tags": ["Weapon"]}}""";
        var handler = new MockHttpMessageHandler(js);
        var client = new HttpClient(handler);
        var logger = new TestLogger<BazaarPlannerImporter>();
        var importer = new BazaarPlannerImporter(client, logger);

        await importer.FetchItemsAsync();

        logger.HasEntry(LogLevel.Debug, "bytes").ShouldBeTrue();
    }

    [Fact]
    public async Task FetchSkillsAsync_LogsContentLength()
    {
        var js = """export const skills = {"Fireball": {"name": "Fireball", "tier": 1, "tags": ["Mak"]}}""";
        var handler = new MockHttpMessageHandler(js);
        var client = new HttpClient(handler);
        var logger = new TestLogger<BazaarPlannerImporter>();
        var importer = new BazaarPlannerImporter(client, logger);

        await importer.FetchSkillsAsync();

        logger.HasEntry(LogLevel.Debug, "bytes").ShouldBeTrue();
    }

    [Fact]
    public async Task FetchMonstersAsync_LogsContentLength()
    {
        var js = """export const monsters = {"Goblin": {"name": "Goblin", "day": 1, "health": 50}}""";
        var handler = new MockHttpMessageHandler(js);
        var client = new HttpClient(handler);
        var logger = new TestLogger<BazaarPlannerImporter>();
        var importer = new BazaarPlannerImporter(client, logger);

        await importer.FetchMonstersAsync();

        logger.HasEntry(LogLevel.Debug, "bytes").ShouldBeTrue();
    }

    [Fact]
    public async Task FetchItemsAsync_WhenContentEmpty_LogsWarning()
    {
        var handler = new MockHttpMessageHandler("");
        var client = new HttpClient(handler);
        var logger = new TestLogger<BazaarPlannerImporter>();
        var importer = new BazaarPlannerImporter(client, logger);

        await importer.FetchItemsAsync();

        logger.HasEntry(LogLevel.Warning, "empty").ShouldBeTrue();
    }

    // --- DataImportService logging tests ---

    [Fact]
    public async Task ImportAllAsync_LogsItemImportCount()
    {
        using var context = TestDbContextFactory.Create();
        var itemsJs = """export const items = {"Sword": {"name": "Sword", "tier": 0, "tags": ["Weapon", "Small"]}}""";
        var skillsJs = """export const skills = {"Fireball": {"name": "Fireball", "tier": 1, "tags": ["Mak"]}}""";
        var monstersJs = """export const monsters = {}""";
        var handler = new MockHttpMessageHandler()
            .WithResponse("items.js", itemsJs)
            .WithResponse("skills.js", skillsJs)
            .WithResponse("monsters.js", monstersJs);
        var client = new HttpClient(handler);
        var importerLogger = new TestLogger<BazaarPlannerImporter>();
        var importer = new BazaarPlannerImporter(client, importerLogger);
        var serviceLogger = new TestLogger<DataImportService>();
        var service = new DataImportService(context, importer, serviceLogger);

        await service.ImportAllAsync();

        serviceLogger.HasEntry(LogLevel.Information, "Imported").ShouldBeTrue();
    }

    [Fact]
    public async Task ImportAllAsync_WhenItemsAlreadyExist_LogsSkippedCount()
    {
        using var context = TestDbContextFactory.Create();
        var itemsJs = """export const items = {"Sword": {"name": "Sword", "tier": 0, "tags": ["Weapon", "Small"]}}""";
        var skillsJs = """export const skills = {}""";
        var monstersJs = """export const monsters = {}""";
        var handler = new MockHttpMessageHandler()
            .WithResponse("items.js", itemsJs)
            .WithResponse("skills.js", skillsJs)
            .WithResponse("monsters.js", monstersJs);
        var client = new HttpClient(handler);
        var importer = new BazaarPlannerImporter(client, NullLogger<BazaarPlannerImporter>.Instance);
        var serviceLogger = new TestLogger<DataImportService>();
        var service = new DataImportService(context, importer, serviceLogger);

        // Import twice - second time items should be skipped
        await service.ImportAllAsync();
        serviceLogger.Entries.Clear();
        await service.ImportAllAsync();

        serviceLogger.HasEntry(LogLevel.Information, "skipped").ShouldBeTrue();
    }

    [Fact]
    public async Task ImportAllAsync_WhenMonsterReferencesUnknownItem_LogsWarning()
    {
        using var context = TestDbContextFactory.Create();
        var itemsJs = """export const items = {}""";
        var skillsJs = """export const skills = {}""";
        var monstersJs = """export const monsters = {"Goblin": {"name": "Goblin", "day": 1, "health": 50, "items": [{"name": "NonExistentItem", "tier": 0}], "skills": []}}""";
        var handler = new MockHttpMessageHandler()
            .WithResponse("items.js", itemsJs)
            .WithResponse("skills.js", skillsJs)
            .WithResponse("monsters.js", monstersJs);
        var client = new HttpClient(handler);
        var importer = new BazaarPlannerImporter(client, NullLogger<BazaarPlannerImporter>.Instance);
        var serviceLogger = new TestLogger<DataImportService>();
        var service = new DataImportService(context, importer, serviceLogger);

        await service.ImportAllAsync();

        serviceLogger.HasEntry(LogLevel.Warning, "NonExistentItem").ShouldBeTrue();
    }

    // --- Existing tests below ---

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
        result[0].Day.ShouldBe("1");
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
    public void ParseJsExport_WithNumericCooldown_ParsesItems()
    {
        // Real BazaarPlanner data has cooldown as number (4) not string ("4")
        var js = """
            export const items = {
                "Abacus": {"name": "Abacus", "tier": 2, "tags": ["Small", "Tool"], "cooldown": 4, "ammo": null, "text": ["Adjacent items gain value"], "enchants": {"Golden": "double value"}}
            };
            """;

        var result = BazaarPlannerImporter.ParseJsExport<BazaarPlannerItem>(js);

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Abacus");
        result[0].Cooldown.ShouldBe("4");
    }

    [Fact]
    public void ParseJsExport_WithMixedCooldownTypes_ParsesAllItems()
    {
        // Items have cooldown as null, string "(5/4)", or number 4
        var js = """
            export const items = {
                "Fitness": {"name": "Fitness", "tier": 2, "tags": ["Large"], "cooldown": null, "ammo": null},
                "Printer": {"name": "Printer", "tier": 2, "tags": ["Large"], "cooldown": "(5/4)", "ammo": null},
                "Abacus": {"name": "Abacus", "tier": 2, "tags": ["Small"], "cooldown": 4, "ammo": null}
            };
            """;

        var result = BazaarPlannerImporter.ParseJsExport<BazaarPlannerItem>(js);

        result.Count.ShouldBe(3);
    }

    [Fact]
    public void ParseJsExport_WithMonsterIdField_ParsesMonsters()
    {
        // Real BazaarPlanner monsters have an "id" field not in our record
        var js = """
            export const monsters = {
                "Fanged Inglet": {"id": "bb1e3506-3735-4669-be90-915a55a7ee05", "name": "Fanged Inglet", "day": 1, "health": 100, "skills": [{"name": "Deadly Eye", "tier": 0}], "items": [{"name": "Pelt", "tier": 0}]}
            };
            """;

        var result = BazaarPlannerImporter.ParseJsExport<BazaarPlannerMonster>(js);

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Fanged Inglet");
        result[0].Health.ShouldBe(100);
    }

    [Fact]
    public void ParseJsExport_WithMonsterItemEnchant_ParsesMonsters()
    {
        // Some monster items have an extra "enchant" field
        var js = """
            export const monsters = {
                "Viper": {"id": "b79845a7", "name": "Viper", "day": 1, "health": 75, "skills": [{"name": "Lash Out", "tier": 0}], "items": [{"name": "Fang", "tier": 0, "enchant": "Toxic"}]}
            };
            """;

        var result = BazaarPlannerImporter.ParseJsExport<BazaarPlannerMonster>(js);

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Viper");
        result[0].Items![0].Name.ShouldBe("Fang");
    }

    [Fact]
    public void ParseJsExport_WithUnquotedPropertyNames_ParsesItems()
    {
        var js = """
            export const items = {
                "Red Envelope": {
                    "name": "Red Envelope",
                    "tier": 0,
                    "tags": ["Common"],
                    "cooldown": null,
                    "ammo": null,
                    "text": ["When you buy this, gain 10 Gold."],
                    enchants: {},
                    priorites: [0]
                }
            };
            """;

        var result = BazaarPlannerImporter.ParseJsExport<BazaarPlannerItem>(js);

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Red Envelope");
    }

    [Fact]
    public void ParseJsExport_WithMonsterDayAsString_ParsesMonsters()
    {
        var js = """
            export const monsters = {
                "Qomatz": {"name": "Qomatz", "day": "event", "health": 200,
                    "skills": [{"name": "Roar", "tier": 1}], "items": []},
                "Goblin": {"name": "Goblin", "day": 1, "health": 50,
                    "skills": [], "items": []}
            };
            """;

        var result = BazaarPlannerImporter.ParseJsExport<BazaarPlannerMonster>(js);

        result.Count.ShouldBe(2);
        result[0].Day.ShouldBe("event");
        result[1].Day.ShouldBe("1");
    }

    [Fact]
    public void ParseJsExport_WithSingleBadEntry_SkipsAndParsesRest()
    {
        var logger = new TestLogger<BazaarPlannerImporter>();
        // Second entry is a non-object value that can't deserialize as BazaarPlannerItem
        var js = """
            export const items = {
                "Sword": {"name": "Sword", "tier": 1, "tags": ["Weapon"]},
                "Bad": "not an object",
                "Shield": {"name": "Shield", "tier": 2, "tags": ["Armor"]}
            };
            """;

        var result = BazaarPlannerImporter.ParseJsExport<BazaarPlannerItem>(js, logger);

        result.Count.ShouldBe(2);
        result[0].Name.ShouldBe("Sword");
        result[1].Name.ShouldBe("Shield");
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

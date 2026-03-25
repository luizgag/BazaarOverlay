using System.Reflection;
using System.Text.Json;
using BazaarOverlay.Application.Interfaces;
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using BazaarOverlay.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BazaarOverlay.Infrastructure.DataImport;

public class DataImportService : IDataImportService
{
    private readonly BazaarDbContext _context;
    private readonly BazaarPlannerImporter _importer;
    private readonly ILogger<DataImportService> _logger;

    public DataImportService(BazaarDbContext context, BazaarPlannerImporter importer, ILogger<DataImportService> logger)
    {
        _context = context;
        _importer = importer;
        _logger = logger;
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

    public async Task SeedRarityProbabilitiesAsync()
    {
        _logger.LogInformation("Seeding rarity probabilities...");
        if (await _context.RarityDayProbabilities.AnyAsync())
        {
            _logger.LogInformation("Rarity probabilities already seeded, skipping");
            return;
        }

        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "BazaarOverlay.Infrastructure.SeedData.rarity-day-probabilities.json";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
            return;

        var entries = await JsonSerializer.DeserializeAsync<List<RarityDayEntry>>(stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (entries is null)
            return;

        foreach (var entry in entries)
        {
            if (Enum.TryParse<Rarity>(entry.Rarity, true, out var rarity))
            {
                _context.RarityDayProbabilities.Add(new RarityDayProbability(entry.Day, rarity, entry.Probability));
            }
        }
        await _context.SaveChangesAsync();
        _logger.LogInformation("Rarity probabilities seeded successfully");
    }

    public async Task ImportAllAsync(IProgress<string>? progress = null)
    {
        _logger.LogInformation("Starting full data import...");
        progress?.Report("Seeding heroes...");
        await SeedHeroesAsync();

        progress?.Report("Seeding rarity probabilities...");
        await SeedRarityProbabilitiesAsync();

        progress?.Report("Fetching items from BazaarPlanner...");
        var bpItems = await _importer.FetchItemsAsync();
        progress?.Report($"Fetched {bpItems.Count} items. Importing...");
        await ImportItemsAsync(bpItems);

        progress?.Report("Fetching skills from BazaarPlanner...");
        var bpSkills = await _importer.FetchSkillsAsync();
        progress?.Report($"Fetched {bpSkills.Count} skills. Importing...");
        await ImportSkillsAsync(bpSkills);

        progress?.Report("Fetching monsters from BazaarPlanner...");
        var bpMonsters = await _importer.FetchMonstersAsync();
        progress?.Report($"Fetched {bpMonsters.Count} monsters. Importing...");
        await ImportMonstersAsync(bpMonsters);

        progress?.Report("Seeding enchantments...");
        await SeedEnchantmentsAsync();

        _logger.LogInformation("Full data import completed");
        progress?.Report("Import complete!");
    }

    private static readonly HashSet<string> HeroNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Dooley", "Jules", "Mak", "Pygmalien", "Stelle", "Vanessa"
    };

    private static readonly HashSet<string> SizeNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Small", "Medium", "Large"
    };

    private async Task ImportItemsAsync(List<BazaarPlannerItem> bpItems)
    {
        var existingHeroes = await _context.Heroes.ToDictionaryAsync(h => h.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var bp in bpItems)
        {
            if (string.IsNullOrWhiteSpace(bp.Name))
                continue;

            if (await _context.Items.AnyAsync(i => i.Name == bp.Name))
                continue;

            var tags = bp.Tags ?? [];
            var heroTags = tags.Where(t => HeroNames.Contains(t)).ToList();
            var sizeTag = tags.FirstOrDefault(t => SizeNames.Contains(t));
            var itemTags = tags.Where(t => !HeroNames.Contains(t) && !SizeNames.Contains(t)).ToList();

            var size = ParseSize(sizeTag);
            var rarity = MapTierToRarity(bp.Tier);
            var cooldown = decimal.TryParse(bp.Cooldown, out var cd) ? cd : (decimal?)null;

            var item = new Item(bp.Name, size, rarity, cooldown);

            foreach (var tag in itemTags)
                item.Tags.Add(new ItemTag(tag));

            if (heroTags.Count > 0)
            {
                foreach (var heroName in heroTags)
                {
                    if (existingHeroes.TryGetValue(heroName, out var hero))
                        item.Heroes.Add(hero);
                }
            }
            else
            {
                if (existingHeroes.TryGetValue("Neutral", out var neutral))
                    item.Heroes.Add(neutral);
            }

            _context.Items.Add(item);
        }

        await _context.SaveChangesAsync();
    }

    private async Task ImportSkillsAsync(List<BazaarPlannerSkill> bpSkills)
    {
        var existingHeroes = await _context.Heroes.ToDictionaryAsync(h => h.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var bp in bpSkills)
        {
            if (string.IsNullOrWhiteSpace(bp.Name))
                continue;

            if (await _context.Skills.AnyAsync(s => s.Name == bp.Name))
                continue;

            var tags = bp.Tags ?? [];
            var heroTags = tags.Where(t => HeroNames.Contains(t)).ToList();
            var skillTags = tags.Where(t => !HeroNames.Contains(t)).ToList();

            var rarity = MapTierToRarity(bp.Tier);
            var skill = new Skill(bp.Name, rarity);

            foreach (var tag in skillTags)
                skill.Tags.Add(new SkillTag(tag));

            if (heroTags.Count > 0)
            {
                foreach (var heroName in heroTags)
                {
                    if (existingHeroes.TryGetValue(heroName, out var hero))
                        skill.Heroes.Add(hero);
                }
            }
            else
            {
                if (existingHeroes.TryGetValue("Neutral", out var neutral))
                    skill.Heroes.Add(neutral);
            }

            _context.Skills.Add(skill);
        }

        await _context.SaveChangesAsync();
    }

    private async Task ImportMonstersAsync(List<BazaarPlannerMonster> bpMonsters)
    {
        foreach (var bp in bpMonsters)
        {
            if (string.IsNullOrWhiteSpace(bp.Name))
                continue;

            if (await _context.Monsters.AnyAsync(m => m.Name == bp.Name))
                continue;

            var health = bp.Health ?? 100;
            var day = bp.Day ?? 1;

            var monster = new Monster(bp.Name, health, Rarity.Bronze, day);

            if (bp.Items is not null)
            {
                foreach (var entry in bp.Items)
                {
                    var item = await _context.Items.FirstOrDefaultAsync(i => i.Name == entry.Name);
                    if (item is not null)
                        monster.DropItems.Add(item);
                }
            }

            if (bp.Skills is not null)
            {
                foreach (var entry in bp.Skills)
                {
                    var skill = await _context.Skills.FirstOrDefaultAsync(s => s.Name == entry.Name);
                    if (skill is not null)
                        monster.DropSkills.Add(skill);
                }
            }

            _context.Monsters.Add(monster);
        }

        await _context.SaveChangesAsync();
    }

    private async Task SeedEnchantmentsAsync()
    {
        var enchantments = new Dictionary<string, string>
        {
            ["Deadly"] = "+50% Crit Chance",
            ["Fiery"] = "Adds/upgrades Burn effect",
            ["Golden"] = "Adds/upgrades Value effect (double value)",
            ["Heavy"] = "Adds/upgrades Slow effect",
            ["Icy"] = "Adds/upgrades Freeze effect",
            ["Obsidian"] = "Adds Damage effect",
            ["Radiant"] = "Grants immunity (cannot be Frozen, Slowed, or Destroyed)",
            ["Restorative"] = "Adds/upgrades Heal effect",
            ["Shielded"] = "Adds/upgrades Shield effect",
            ["Shiny"] = "Adds/upgrades Multicast effect (double multicast)",
            ["Toxic"] = "Adds/upgrades Poison effect",
            ["Turbo"] = "Adds/upgrades Haste effect"
        };

        foreach (var (name, desc) in enchantments)
        {
            if (!await _context.Enchantments.AnyAsync(e => e.Name == name))
                _context.Enchantments.Add(new Enchantment(name, desc));
        }

        await _context.SaveChangesAsync();
    }

    private static ItemSize ParseSize(string? size) => size?.ToLower() switch
    {
        "small" => ItemSize.Small,
        "medium" => ItemSize.Medium,
        "large" => ItemSize.Large,
        _ => ItemSize.Small
    };

    private static Rarity MapTierToRarity(int? tier) => tier switch
    {
        0 => Rarity.Bronze,
        1 => Rarity.Silver,
        2 => Rarity.Gold,
        3 => Rarity.Diamond,
        4 => Rarity.Legendary,
        _ => Rarity.Bronze
    };

    private record RarityDayEntry(int Day, string Rarity, decimal Probability);
}

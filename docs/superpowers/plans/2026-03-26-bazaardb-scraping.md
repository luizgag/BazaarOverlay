# BazaarDB.gg Web Scraping Migration — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace BazaarPlanner GitHub import with bazaardb.gg web scraping via PuppeteerSharp, using distinct domain entities for Monsters, Merchants, Trainers, and Events.

**Architecture:** Onion/DDD with PuppeteerSharp scrapers in Infrastructure layer. One scraper per entity type, orchestrated by BazaarDbScraper. Full wipe-and-reimport: delete DB, recreate schema, seed static data, scrape all categories page-by-page.

**Tech Stack:** .NET 10, EF Core 10, PuppeteerSharp, SQLite, xUnit + Shouldly + NSubstitute

---

## File Map

### Files to Delete

```
src/BazaarOverlay.Domain/Entities/Encounter.cs
src/BazaarOverlay.Domain/Entities/ShopConstraints.cs
src/BazaarOverlay.Domain/Entities/ShopAllowedTag.cs
src/BazaarOverlay.Domain/Entities/RarityDayProbability.cs
src/BazaarOverlay.Domain/Enums/EncounterType.cs
src/BazaarOverlay.Domain/Interfaces/IEncounterRepository.cs
src/BazaarOverlay.Domain/Interfaces/IRarityDayProbabilityRepository.cs
src/BazaarOverlay.Application/DTOs/EncounterResult.cs
src/BazaarOverlay.Application/DTOs/MonsterEncounterResult.cs
src/BazaarOverlay.Application/DTOs/ShopResult.cs
src/BazaarOverlay.Application/Interfaces/IEncounterService.cs
src/BazaarOverlay.Application/Interfaces/IMonsterEncounterService.cs
src/BazaarOverlay.Application/Interfaces/IShopService.cs
src/BazaarOverlay.Application/Services/EncounterService.cs
src/BazaarOverlay.Application/Services/MonsterEncounterService.cs
src/BazaarOverlay.Application/Services/ShopService.cs
src/BazaarOverlay.Infrastructure/DataImport/BazaarPlannerImporter.cs
src/BazaarOverlay.Infrastructure/Persistence/Repositories/EncounterRepository.cs
src/BazaarOverlay.Infrastructure/Persistence/Repositories/RarityDayProbabilityRepository.cs
src/BazaarOverlay.Infrastructure/SeedData/rarity-day-probabilities.json
tests/BazaarOverlay.Tests/Application/EncounterServiceTests.cs
tests/BazaarOverlay.Tests/Application/MonsterEncounterServiceTests.cs
tests/BazaarOverlay.Tests/Application/ShopServiceTests.cs
tests/BazaarOverlay.Tests/Domain/RarityDayProbabilityTests.cs
tests/BazaarOverlay.Tests/Infrastructure/RarityDayProbabilityRepositoryTests.cs
```

### Files to Create

```
src/BazaarOverlay.Domain/Entities/Merchant.cs
src/BazaarOverlay.Domain/Entities/Trainer.cs
src/BazaarOverlay.Domain/Entities/Event.cs
src/BazaarOverlay.Domain/Entities/EventOption.cs
src/BazaarOverlay.Domain/Entities/MonsterTag.cs
src/BazaarOverlay.Domain/Interfaces/IMerchantRepository.cs
src/BazaarOverlay.Domain/Interfaces/ITrainerRepository.cs
src/BazaarOverlay.Domain/Interfaces/IEventRepository.cs
src/BazaarOverlay.Application/DTOs/MonsterResult.cs
src/BazaarOverlay.Application/DTOs/MerchantResult.cs
src/BazaarOverlay.Application/DTOs/TrainerResult.cs
src/BazaarOverlay.Application/DTOs/EventResult.cs
src/BazaarOverlay.Application/Interfaces/IMonsterService.cs
src/BazaarOverlay.Application/Interfaces/IMerchantService.cs
src/BazaarOverlay.Application/Interfaces/ITrainerService.cs
src/BazaarOverlay.Application/Interfaces/IEventService.cs
src/BazaarOverlay.Application/Services/MonsterService.cs
src/BazaarOverlay.Application/Services/MerchantService.cs
src/BazaarOverlay.Application/Services/TrainerService.cs
src/BazaarOverlay.Application/Services/EventService.cs
src/BazaarOverlay.Infrastructure/Scraping/IBazaarDbScraper.cs
src/BazaarOverlay.Infrastructure/Scraping/BazaarDbScraper.cs
src/BazaarOverlay.Infrastructure/Scraping/ScraperBase.cs
src/BazaarOverlay.Infrastructure/Scraping/ItemScraper.cs
src/BazaarOverlay.Infrastructure/Scraping/SkillScraper.cs
src/BazaarOverlay.Infrastructure/Scraping/MonsterScraper.cs
src/BazaarOverlay.Infrastructure/Scraping/MerchantScraper.cs
src/BazaarOverlay.Infrastructure/Scraping/TrainerScraper.cs
src/BazaarOverlay.Infrastructure/Scraping/EventScraper.cs
src/BazaarOverlay.Infrastructure/Persistence/Repositories/MerchantRepository.cs
src/BazaarOverlay.Infrastructure/Persistence/Repositories/TrainerRepository.cs
src/BazaarOverlay.Infrastructure/Persistence/Repositories/EventRepository.cs
tests/BazaarOverlay.Tests/Domain/MerchantTests.cs
tests/BazaarOverlay.Tests/Domain/TrainerTests.cs
tests/BazaarOverlay.Tests/Domain/EventTests.cs
tests/BazaarOverlay.Tests/Domain/EventOptionTests.cs
tests/BazaarOverlay.Tests/Application/MonsterServiceTests.cs
tests/BazaarOverlay.Tests/Application/MerchantServiceTests.cs
tests/BazaarOverlay.Tests/Application/TrainerServiceTests.cs
tests/BazaarOverlay.Tests/Application/EventServiceTests.cs
tests/BazaarOverlay.Tests/Infrastructure/MerchantRepositoryTests.cs
tests/BazaarOverlay.Tests/Infrastructure/TrainerRepositoryTests.cs
tests/BazaarOverlay.Tests/Infrastructure/EventRepositoryTests.cs
```

### Files to Modify

```
src/BazaarOverlay.Domain/Entities/Hero.cs               — add Abbreviation field
src/BazaarOverlay.Domain/Entities/Item.cs                — add Cost, Value, Description, BazaarDbId; remove IsAvailableOnDay
src/BazaarOverlay.Domain/Entities/Skill.cs               — add Cost, Description, BazaarDbId
src/BazaarOverlay.Domain/Entities/Monster.cs             — rewrite: new fields, BoardItems/BoardSkills
src/BazaarOverlay.Infrastructure/Persistence/BazaarDbContext.cs  — remove old configs, add new entities
src/BazaarOverlay.Infrastructure/DataImport/DataImportService.cs — rewrite to use IBazaarDbScraper
src/BazaarOverlay.Infrastructure/Persistence/Repositories/MonsterRepository.cs — rewrite for new entity
src/BazaarOverlay.Infrastructure/BazaarOverlay.Infrastructure.csproj — add PuppeteerSharp
src/BazaarOverlay.Application/DTOs/ItemInfoResult.cs     — add Cost, Value, Description
src/BazaarOverlay.Application/DTOs/SkillInfoResult.cs    — add Cost, Description
src/BazaarOverlay.Application/Interfaces/IDataImportService.cs — simplify (remove SeedRarity)
src/BazaarOverlay.Application/Services/ItemInfoService.cs — map new fields
src/BazaarOverlay.Application/Services/SkillInfoService.cs — map new fields
src/BazaarOverlay.WPF/App.xaml.cs                        — update DI registrations
src/BazaarOverlay.WPF/ViewModels/MainViewModel.cs        — remove rarity seeding from startup
src/BazaarOverlay.WPF/ViewModels/MonsterEncounterViewModel.cs — use new IMonsterService
tests/BazaarOverlay.Tests/Domain/HeroTests.cs            — add Abbreviation tests
tests/BazaarOverlay.Tests/Domain/ItemTests.cs            — update for new fields, remove IsAvailableOnDay tests
tests/BazaarOverlay.Tests/Domain/MonsterTests.cs         — rewrite for new entity
tests/BazaarOverlay.Tests/Infrastructure/MonsterRepositoryTests.cs — rewrite for new entity
tests/BazaarOverlay.Tests/Application/ItemInfoServiceTests.cs — update for new fields
tests/BazaarOverlay.Tests/Application/SkillInfoServiceTests.cs — update for new fields
tests/BazaarOverlay.Tests/Infrastructure/TestDbContextFactory.cs — no change needed (auto-adapts)
```

---

## Task 1: Delete Obsolete Code

**Files:**
- Delete: all files listed in "Files to Delete" above

This task removes all encounter-related code, BazaarPlanner importer, rarity probability system, and their tests. The project will not compile after this task — that's expected. Subsequent tasks restore compilation.

- [ ] **Step 1: Delete obsolete domain entities and enums**

```bash
rm src/BazaarOverlay.Domain/Entities/Encounter.cs
rm src/BazaarOverlay.Domain/Entities/ShopConstraints.cs
rm src/BazaarOverlay.Domain/Entities/ShopAllowedTag.cs
rm src/BazaarOverlay.Domain/Entities/RarityDayProbability.cs
rm src/BazaarOverlay.Domain/Enums/EncounterType.cs
rm src/BazaarOverlay.Domain/Interfaces/IEncounterRepository.cs
rm src/BazaarOverlay.Domain/Interfaces/IRarityDayProbabilityRepository.cs
```

- [ ] **Step 2: Delete obsolete application DTOs, interfaces, and services**

```bash
rm src/BazaarOverlay.Application/DTOs/EncounterResult.cs
rm src/BazaarOverlay.Application/DTOs/MonsterEncounterResult.cs
rm src/BazaarOverlay.Application/DTOs/ShopResult.cs
rm src/BazaarOverlay.Application/Interfaces/IEncounterService.cs
rm src/BazaarOverlay.Application/Interfaces/IMonsterEncounterService.cs
rm src/BazaarOverlay.Application/Interfaces/IShopService.cs
rm src/BazaarOverlay.Application/Services/EncounterService.cs
rm src/BazaarOverlay.Application/Services/MonsterEncounterService.cs
rm src/BazaarOverlay.Application/Services/ShopService.cs
```

- [ ] **Step 3: Delete obsolete infrastructure code**

```bash
rm src/BazaarOverlay.Infrastructure/DataImport/BazaarPlannerImporter.cs
rm src/BazaarOverlay.Infrastructure/Persistence/Repositories/EncounterRepository.cs
rm src/BazaarOverlay.Infrastructure/Persistence/Repositories/RarityDayProbabilityRepository.cs
rm src/BazaarOverlay.Infrastructure/SeedData/rarity-day-probabilities.json
```

- [ ] **Step 4: Delete obsolete tests**

```bash
rm tests/BazaarOverlay.Tests/Application/EncounterServiceTests.cs
rm tests/BazaarOverlay.Tests/Application/MonsterEncounterServiceTests.cs
rm tests/BazaarOverlay.Tests/Application/ShopServiceTests.cs
rm tests/BazaarOverlay.Tests/Domain/RarityDayProbabilityTests.cs
rm tests/BazaarOverlay.Tests/Infrastructure/RarityDayProbabilityRepositoryTests.cs
```

- [ ] **Step 5: Remove the embedded resource entry from Infrastructure .csproj**

Check if `BazaarOverlay.Infrastructure.csproj` has an `<EmbeddedResource>` entry for the rarity JSON. If so, remove it.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "Remove obsolete encounter/rarity/BazaarPlanner code for bazaardb.gg migration"
```

---

## Task 2: Modify Hero Entity

**Files:**
- Modify: `src/BazaarOverlay.Domain/Entities/Hero.cs`
- Test: `tests/BazaarOverlay.Tests/Domain/HeroTests.cs`

- [ ] **Step 1: Write failing test for Hero Abbreviation**

Add to `tests/BazaarOverlay.Tests/Domain/HeroTests.cs`:

```csharp
[Fact]
public void Constructor_WithAbbreviation_SetsAbbreviation()
{
    var hero = new Hero("Karnok", "KAR");

    hero.Name.ShouldBe("Karnok");
    hero.Abbreviation.ShouldBe("KAR");
}

[Fact]
public void Constructor_WithoutAbbreviation_DefaultsToEmptyString()
{
    var hero = new Hero("Karnok");

    hero.Abbreviation.ShouldBe(string.Empty);
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/BazaarOverlay.Tests --filter "HeroTests" --no-restore
```

Expected: FAIL — `Hero` constructor doesn't accept abbreviation parameter.

- [ ] **Step 3: Implement Hero changes**

Replace `src/BazaarOverlay.Domain/Entities/Hero.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace BazaarOverlay.Domain.Entities;

public class Hero
{
    [Key]
    [Required]
    [MaxLength(50)]
    public string Name { get; private set; } = string.Empty;

    [MaxLength(3)]
    public string Abbreviation { get; private set; } = string.Empty;

    public ICollection<Item> Items { get; private set; } = new List<Item>();
    public ICollection<Skill> Skills { get; private set; } = new List<Skill>();

    private Hero() { }

    public Hero(string name, string abbreviation = "")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Hero name cannot be empty.", nameof(name));

        Name = name.Trim();
        Abbreviation = abbreviation.Trim();
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

```bash
dotnet test tests/BazaarOverlay.Tests --filter "HeroTests" --no-restore
```

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/BazaarOverlay.Domain/Entities/Hero.cs tests/BazaarOverlay.Tests/Domain/HeroTests.cs
git commit -m "Add Abbreviation field to Hero entity"
```

---

## Task 3: Modify Item Entity

**Files:**
- Modify: `src/BazaarOverlay.Domain/Entities/Item.cs`
- Test: `tests/BazaarOverlay.Tests/Domain/ItemTests.cs`

- [ ] **Step 1: Write failing tests for new Item fields**

Replace `tests/BazaarOverlay.Tests/Domain/ItemTests.cs`:

```csharp
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using Shouldly;

namespace BazaarOverlay.Tests.Domain;

public class ItemTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesItem()
    {
        var item = new Item("Rusty Sword", ItemSize.Small, Rarity.Bronze, cooldown: 4.0m);

        item.Name.ShouldBe("Rusty Sword");
        item.Size.ShouldBe(ItemSize.Small);
        item.MinimumRarity.ShouldBe(Rarity.Bronze);
        item.Cooldown.ShouldBe(4.0m);
    }

    [Fact]
    public void Constructor_WithAllFields_SetsAllProperties()
    {
        var item = new Item("Trapping Pit", ItemSize.Medium, Rarity.Silver,
            cooldown: 6.0m, cost: "8 >> 16 >> 32", value: "4 >> 8 >> 16",
            description: "Destroy enemy item", bazaarDbId: "py8ycyzvx7yg6xk05lj73jdyx");

        item.Cost.ShouldBe("8 >> 16 >> 32");
        item.Value.ShouldBe("4 >> 8 >> 16");
        item.Description.ShouldBe("Destroy enemy item");
        item.BazaarDbId.ShouldBe("py8ycyzvx7yg6xk05lj73jdyx");
    }

    [Fact]
    public void Constructor_WithNullOptionalFields_AllowsNulls()
    {
        var item = new Item("Shield", ItemSize.Medium, Rarity.Bronze);

        item.Cooldown.ShouldBeNull();
        item.Cost.ShouldBeNull();
        item.Value.ShouldBeNull();
        item.Description.ShouldBeNull();
        item.BazaarDbId.ShouldBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidName_ThrowsArgumentException(string? name)
    {
        Should.Throw<ArgumentException>(() => new Item(name!, ItemSize.Small, Rarity.Bronze));
    }

    [Fact]
    public void IsAvailableForHero_WithMatchingHero_ReturnsTrue()
    {
        var item = new Item("Vanessa's Blade", ItemSize.Small, Rarity.Bronze);
        var hero = new Hero("Vanessa");
        item.Heroes.Add(hero);

        item.IsAvailableForHero("Vanessa").ShouldBeTrue();
    }

    [Fact]
    public void IsAvailableForHero_WithNeutralHero_ReturnsTrue()
    {
        var item = new Item("Generic Sword", ItemSize.Small, Rarity.Bronze);
        var neutral = new Hero("Neutral");
        item.Heroes.Add(neutral);

        item.IsAvailableForHero("Vanessa").ShouldBeTrue();
    }

    [Fact]
    public void IsAvailableForHero_WithDifferentHero_ReturnsFalse()
    {
        var item = new Item("Vanessa's Blade", ItemSize.Small, Rarity.Bronze);
        var hero = new Hero("Vanessa");
        item.Heroes.Add(hero);

        item.IsAvailableForHero("Dooley").ShouldBeFalse();
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/BazaarOverlay.Tests --filter "ItemTests" --no-restore
```

Expected: FAIL — `Item` constructor doesn't accept `cost`, `value`, `description`, `bazaarDbId`.

- [ ] **Step 3: Implement Item changes**

Replace `src/BazaarOverlay.Domain/Entities/Item.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using BazaarOverlay.Domain.Enums;

namespace BazaarOverlay.Domain.Entities;

public class Item
{
    [Key]
    public int Id { get; private set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; private set; } = string.Empty;

    [Required]
    public ItemSize Size { get; private set; }

    public decimal? Cooldown { get; private set; }

    [Required]
    public Rarity MinimumRarity { get; private set; }

    [MaxLength(100)]
    public string? Cost { get; private set; }

    [MaxLength(100)]
    public string? Value { get; private set; }

    [MaxLength(500)]
    public string? Description { get; private set; }

    [MaxLength(50)]
    public string? BazaarDbId { get; private set; }

    public ICollection<ItemTag> Tags { get; private set; } = new List<ItemTag>();
    public ICollection<Hero> Heroes { get; private set; } = new List<Hero>();
    public ICollection<ItemTierValue> TierValues { get; private set; } = new List<ItemTierValue>();
    public ICollection<ItemEnchantment> Enchantments { get; private set; } = new List<ItemEnchantment>();

    private Item() { }

    public Item(string name, ItemSize size, Rarity minimumRarity, decimal? cooldown = null,
        string? cost = null, string? value = null, string? description = null, string? bazaarDbId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Item name cannot be empty.", nameof(name));

        Name = name.Trim();
        Size = size;
        MinimumRarity = minimumRarity;
        Cooldown = cooldown;
        Cost = cost?.Trim();
        Value = value?.Trim();
        Description = description?.Trim();
        BazaarDbId = bazaarDbId?.Trim();
    }

    public bool IsAvailableForHero(string heroName)
    {
        return Heroes.Any(h => h.Name == heroName || h.Name == "Neutral");
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

```bash
dotnet test tests/BazaarOverlay.Tests --filter "ItemTests" --no-restore
```

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/BazaarOverlay.Domain/Entities/Item.cs tests/BazaarOverlay.Tests/Domain/ItemTests.cs
git commit -m "Add Cost, Value, Description, BazaarDbId fields to Item entity"
```

---

## Task 4: Modify Skill Entity

**Files:**
- Modify: `src/BazaarOverlay.Domain/Entities/Skill.cs`
- Test: (add to existing test file or create if needed)

- [ ] **Step 1: Write failing test for new Skill fields**

Create or update `tests/BazaarOverlay.Tests/Domain/SkillTests.cs`:

```csharp
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using Shouldly;

namespace BazaarOverlay.Tests.Domain;

public class SkillTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesSkill()
    {
        var skill = new Skill("Burning Temper", Rarity.Bronze);

        skill.Name.ShouldBe("Burning Temper");
        skill.MinimumRarity.ShouldBe(Rarity.Bronze);
    }

    [Fact]
    public void Constructor_WithAllFields_SetsAllProperties()
    {
        var skill = new Skill("Burning Temper", Rarity.Bronze,
            cost: "5 >> 10 >> 20 >> 40",
            description: "While Enraged, Burn items have +3 Burn",
            bazaarDbId: "304tdnf7npqk1kbshhxx48p3fj");

        skill.Cost.ShouldBe("5 >> 10 >> 20 >> 40");
        skill.Description.ShouldBe("While Enraged, Burn items have +3 Burn");
        skill.BazaarDbId.ShouldBe("304tdnf7npqk1kbshhxx48p3fj");
    }

    [Fact]
    public void Constructor_WithNullOptionalFields_AllowsNulls()
    {
        var skill = new Skill("Burning Temper", Rarity.Bronze);

        skill.Cost.ShouldBeNull();
        skill.Description.ShouldBeNull();
        skill.BazaarDbId.ShouldBeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidName_ThrowsArgumentException(string? name)
    {
        Should.Throw<ArgumentException>(() => new Skill(name!, Rarity.Bronze));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/BazaarOverlay.Tests --filter "SkillTests" --no-restore
```

Expected: FAIL — `Skill` constructor doesn't accept `cost`, `description`, `bazaarDbId`.

- [ ] **Step 3: Implement Skill changes**

Replace `src/BazaarOverlay.Domain/Entities/Skill.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using BazaarOverlay.Domain.Enums;

namespace BazaarOverlay.Domain.Entities;

public class Skill
{
    [Key]
    public int Id { get; private set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; private set; } = string.Empty;

    [Required]
    public Rarity MinimumRarity { get; private set; }

    [MaxLength(100)]
    public string? Cost { get; private set; }

    [MaxLength(500)]
    public string? Description { get; private set; }

    [MaxLength(50)]
    public string? BazaarDbId { get; private set; }

    public ICollection<SkillTag> Tags { get; private set; } = new List<SkillTag>();
    public ICollection<Hero> Heroes { get; private set; } = new List<Hero>();
    public ICollection<SkillTierValue> TierValues { get; private set; } = new List<SkillTierValue>();

    private Skill() { }

    public Skill(string name, Rarity minimumRarity, string? cost = null,
        string? description = null, string? bazaarDbId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Skill name cannot be empty.", nameof(name));

        Name = name.Trim();
        MinimumRarity = minimumRarity;
        Cost = cost?.Trim();
        Description = description?.Trim();
        BazaarDbId = bazaarDbId?.Trim();
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

```bash
dotnet test tests/BazaarOverlay.Tests --filter "SkillTests" --no-restore
```

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/BazaarOverlay.Domain/Entities/Skill.cs tests/BazaarOverlay.Tests/Domain/SkillTests.cs
git commit -m "Add Cost, Description, BazaarDbId fields to Skill entity"
```

---

## Task 5: Rewrite Monster Entity

**Files:**
- Modify: `src/BazaarOverlay.Domain/Entities/Monster.cs`
- Test: `tests/BazaarOverlay.Tests/Domain/MonsterTests.cs`

- [ ] **Step 1: Write failing tests for new Monster entity**

Replace `tests/BazaarOverlay.Tests/Domain/MonsterTests.cs`:

```csharp
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using Shouldly;

namespace BazaarOverlay.Tests.Domain;

public class MonsterTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesMonster()
    {
        var monster = new Monster("Banannibal", Rarity.Bronze, level: 1, day: 1,
            health: 100, goldReward: 2, xpReward: 3);

        monster.Name.ShouldBe("Banannibal");
        monster.Tier.ShouldBe(Rarity.Bronze);
        monster.Level.ShouldBe(1);
        monster.Day.ShouldBe(1);
        monster.Health.ShouldBe(100);
        monster.GoldReward.ShouldBe(2);
        monster.XpReward.ShouldBe(3);
    }

    [Fact]
    public void Constructor_WithBazaarDbId_SetsId()
    {
        var monster = new Monster("Banannibal", Rarity.Bronze, level: 1, day: 1,
            health: 100, goldReward: 2, xpReward: 3,
            bazaarDbId: "4k4n5d9g1c9ydpt7c1gy7wg72q");

        monster.BazaarDbId.ShouldBe("4k4n5d9g1c9ydpt7c1gy7wg72q");
    }

    [Fact]
    public void Constructor_TrimsName()
    {
        var monster = new Monster("  Banannibal  ", Rarity.Bronze, level: 1, day: 1,
            health: 100, goldReward: 2, xpReward: 3);

        monster.Name.ShouldBe("Banannibal");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidName_ThrowsArgumentException(string? name)
    {
        Should.Throw<ArgumentException>(() => new Monster(name!, Rarity.Bronze,
            level: 1, day: 1, health: 100, goldReward: 2, xpReward: 3));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithInvalidHealth_ThrowsArgumentException(int health)
    {
        Should.Throw<ArgumentException>(() => new Monster("Goblin", Rarity.Bronze,
            level: 1, day: 1, health: health, goldReward: 2, xpReward: 3));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithInvalidDay_ThrowsArgumentException(int day)
    {
        Should.Throw<ArgumentException>(() => new Monster("Goblin", Rarity.Bronze,
            level: 1, day: day, health: 100, goldReward: 2, xpReward: 3));
    }

    [Fact]
    public void BoardItems_StartsEmpty()
    {
        var monster = new Monster("Banannibal", Rarity.Bronze, level: 1, day: 1,
            health: 100, goldReward: 2, xpReward: 3);

        monster.BoardItems.ShouldBeEmpty();
        monster.BoardSkills.ShouldBeEmpty();
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/BazaarOverlay.Tests --filter "MonsterTests" --no-restore
```

Expected: FAIL — constructor signature doesn't match.

- [ ] **Step 3: Implement new Monster entity**

Replace `src/BazaarOverlay.Domain/Entities/Monster.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using BazaarOverlay.Domain.Enums;

namespace BazaarOverlay.Domain.Entities;

public class Monster
{
    [Key]
    public int Id { get; private set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; private set; } = string.Empty;

    [Required]
    public Rarity Tier { get; private set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int Level { get; private set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int Day { get; private set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int Health { get; private set; }

    [Range(0, int.MaxValue)]
    public int GoldReward { get; private set; }

    [Range(0, int.MaxValue)]
    public int XpReward { get; private set; }

    [MaxLength(50)]
    public string? BazaarDbId { get; private set; }

    public ICollection<Item> BoardItems { get; private set; } = new List<Item>();
    public ICollection<Skill> BoardSkills { get; private set; } = new List<Skill>();

    private Monster() { }

    public Monster(string name, Rarity tier, int level, int day, int health,
        int goldReward, int xpReward, string? bazaarDbId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Monster name cannot be empty.", nameof(name));
        if (health <= 0)
            throw new ArgumentException("Health must be positive.", nameof(health));
        if (day <= 0)
            throw new ArgumentException("Day must be positive.", nameof(day));

        Name = name.Trim();
        Tier = tier;
        Level = level;
        Day = day;
        Health = health;
        GoldReward = goldReward;
        XpReward = xpReward;
        BazaarDbId = bazaarDbId?.Trim();
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

```bash
dotnet test tests/BazaarOverlay.Tests --filter "MonsterTests" --no-restore
```

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/BazaarOverlay.Domain/Entities/Monster.cs tests/BazaarOverlay.Tests/Domain/MonsterTests.cs
git commit -m "Rewrite Monster entity with tier, level, day, rewards, board collections"
```

---

## Task 6: Create Merchant Entity

**Files:**
- Create: `src/BazaarOverlay.Domain/Entities/Merchant.cs`
- Test: `tests/BazaarOverlay.Tests/Domain/MerchantTests.cs`

- [ ] **Step 1: Write failing test**

Create `tests/BazaarOverlay.Tests/Domain/MerchantTests.cs`:

```csharp
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using Shouldly;

namespace BazaarOverlay.Tests.Domain;

public class MerchantTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesMerchant()
    {
        var merchant = new Merchant("Barkun", Rarity.Silver,
            tooltip: "Sells Medium and Large items.",
            bazaarDbId: "qf6h07kp9vmw5mcym5m3wdtbny");

        merchant.Name.ShouldBe("Barkun");
        merchant.Tier.ShouldBe(Rarity.Silver);
        merchant.Tooltip.ShouldBe("Sells Medium and Large items.");
        merchant.BazaarDbId.ShouldBe("qf6h07kp9vmw5mcym5m3wdtbny");
    }

    [Fact]
    public void Constructor_WithRules_SetsAllFields()
    {
        var merchant = new Merchant("Barkun", Rarity.Silver,
            tooltip: "Sells items",
            selectionRule: "You are able to select multiple items.",
            costRule: "You must pay the cost.",
            leaveRule: "You can leave.",
            rerollCount: 1, rerollCost: 4);

        merchant.SelectionRule.ShouldBe("You are able to select multiple items.");
        merchant.CostRule.ShouldBe("You must pay the cost.");
        merchant.LeaveRule.ShouldBe("You can leave.");
        merchant.RerollCount.ShouldBe(1);
        merchant.RerollCost.ShouldBe(4);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidName_ThrowsArgumentException(string? name)
    {
        Should.Throw<ArgumentException>(() => new Merchant(name!, Rarity.Silver, "tooltip"));
    }

    [Fact]
    public void ItemPool_StartsEmpty()
    {
        var merchant = new Merchant("Barkun", Rarity.Silver, tooltip: "Sells items");

        merchant.ItemPool.ShouldBeEmpty();
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/BazaarOverlay.Tests --filter "MerchantTests" --no-restore
```

Expected: FAIL — `Merchant` class doesn't exist.

- [ ] **Step 3: Implement Merchant entity**

Create `src/BazaarOverlay.Domain/Entities/Merchant.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using BazaarOverlay.Domain.Enums;

namespace BazaarOverlay.Domain.Entities;

public class Merchant
{
    [Key]
    public int Id { get; private set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; private set; } = string.Empty;

    [Required]
    public Rarity Tier { get; private set; }

    [MaxLength(500)]
    public string? Tooltip { get; private set; }

    [MaxLength(200)]
    public string? SelectionRule { get; private set; }

    [MaxLength(200)]
    public string? CostRule { get; private set; }

    [MaxLength(200)]
    public string? LeaveRule { get; private set; }

    public int? RerollCount { get; private set; }
    public int? RerollCost { get; private set; }

    [MaxLength(50)]
    public string? BazaarDbId { get; private set; }

    public ICollection<Item> ItemPool { get; private set; } = new List<Item>();

    private Merchant() { }

    public Merchant(string name, Rarity tier, string? tooltip = null,
        string? selectionRule = null, string? costRule = null, string? leaveRule = null,
        int? rerollCount = null, int? rerollCost = null, string? bazaarDbId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Merchant name cannot be empty.", nameof(name));

        Name = name.Trim();
        Tier = tier;
        Tooltip = tooltip?.Trim();
        SelectionRule = selectionRule?.Trim();
        CostRule = costRule?.Trim();
        LeaveRule = leaveRule?.Trim();
        RerollCount = rerollCount;
        RerollCost = rerollCost;
        BazaarDbId = bazaarDbId?.Trim();
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

```bash
dotnet test tests/BazaarOverlay.Tests --filter "MerchantTests" --no-restore
```

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/BazaarOverlay.Domain/Entities/Merchant.cs tests/BazaarOverlay.Tests/Domain/MerchantTests.cs
git commit -m "Create Merchant domain entity"
```

---

## Task 7: Create Trainer Entity

**Files:**
- Create: `src/BazaarOverlay.Domain/Entities/Trainer.cs`
- Test: `tests/BazaarOverlay.Tests/Domain/TrainerTests.cs`

- [ ] **Step 1: Write failing test**

Create `tests/BazaarOverlay.Tests/Domain/TrainerTests.cs`:

```csharp
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using Shouldly;

namespace BazaarOverlay.Tests.Domain;

public class TrainerTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesTrainer()
    {
        var trainer = new Trainer("Adira", Rarity.Diamond,
            tooltip: "Teaches Diamond-tier Skills",
            bazaarDbId: "w99h14w3m1sfljzmd2ldfglnh6");

        trainer.Name.ShouldBe("Adira");
        trainer.Tier.ShouldBe(Rarity.Diamond);
        trainer.Tooltip.ShouldBe("Teaches Diamond-tier Skills");
        trainer.BazaarDbId.ShouldBe("w99h14w3m1sfljzmd2ldfglnh6");
    }

    [Fact]
    public void Constructor_WithRules_SetsAllFields()
    {
        var trainer = new Trainer("Adira", Rarity.Diamond,
            tooltip: "Teaches skills",
            selectionRule: "You can only select one skill.",
            costRule: "Skills are always free.",
            leaveRule: "You can leave.",
            rerollCount: 1, rerollCost: 8);

        trainer.SelectionRule.ShouldBe("You can only select one skill.");
        trainer.RerollCount.ShouldBe(1);
        trainer.RerollCost.ShouldBe(8);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidName_ThrowsArgumentException(string? name)
    {
        Should.Throw<ArgumentException>(() => new Trainer(name!, Rarity.Diamond, "tooltip"));
    }

    [Fact]
    public void SkillPool_StartsEmpty()
    {
        var trainer = new Trainer("Adira", Rarity.Diamond, tooltip: "Teaches skills");

        trainer.SkillPool.ShouldBeEmpty();
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/BazaarOverlay.Tests --filter "TrainerTests" --no-restore
```

Expected: FAIL — `Trainer` class doesn't exist.

- [ ] **Step 3: Implement Trainer entity**

Create `src/BazaarOverlay.Domain/Entities/Trainer.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using BazaarOverlay.Domain.Enums;

namespace BazaarOverlay.Domain.Entities;

public class Trainer
{
    [Key]
    public int Id { get; private set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; private set; } = string.Empty;

    [Required]
    public Rarity Tier { get; private set; }

    [MaxLength(500)]
    public string? Tooltip { get; private set; }

    [MaxLength(200)]
    public string? SelectionRule { get; private set; }

    [MaxLength(200)]
    public string? CostRule { get; private set; }

    [MaxLength(200)]
    public string? LeaveRule { get; private set; }

    public int? RerollCount { get; private set; }
    public int? RerollCost { get; private set; }

    [MaxLength(50)]
    public string? BazaarDbId { get; private set; }

    public ICollection<Skill> SkillPool { get; private set; } = new List<Skill>();

    private Trainer() { }

    public Trainer(string name, Rarity tier, string? tooltip = null,
        string? selectionRule = null, string? costRule = null, string? leaveRule = null,
        int? rerollCount = null, int? rerollCost = null, string? bazaarDbId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Trainer name cannot be empty.", nameof(name));

        Name = name.Trim();
        Tier = tier;
        Tooltip = tooltip?.Trim();
        SelectionRule = selectionRule?.Trim();
        CostRule = costRule?.Trim();
        LeaveRule = leaveRule?.Trim();
        RerollCount = rerollCount;
        RerollCost = rerollCost;
        BazaarDbId = bazaarDbId?.Trim();
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

```bash
dotnet test tests/BazaarOverlay.Tests --filter "TrainerTests" --no-restore
```

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add src/BazaarOverlay.Domain/Entities/Trainer.cs tests/BazaarOverlay.Tests/Domain/TrainerTests.cs
git commit -m "Create Trainer domain entity"
```

---

## Task 8: Create Event and EventOption Entities

**Files:**
- Create: `src/BazaarOverlay.Domain/Entities/Event.cs`
- Create: `src/BazaarOverlay.Domain/Entities/EventOption.cs`
- Test: `tests/BazaarOverlay.Tests/Domain/EventTests.cs`
- Test: `tests/BazaarOverlay.Tests/Domain/EventOptionTests.cs`

- [ ] **Step 1: Write failing tests for Event**

Create `tests/BazaarOverlay.Tests/Domain/EventTests.cs`:

```csharp
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using Shouldly;

namespace BazaarOverlay.Tests.Domain;

public class EventTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesEvent()
    {
        var evt = new Event("A Strange Mushroom", Rarity.Bronze,
            tooltip: "You find a strange mushroom.",
            bazaarDbId: "10y2n43q8sd46dcj7k38j5w25vs");

        evt.Name.ShouldBe("A Strange Mushroom");
        evt.Tier.ShouldBe(Rarity.Bronze);
        evt.Tooltip.ShouldBe("You find a strange mushroom.");
        evt.BazaarDbId.ShouldBe("10y2n43q8sd46dcj7k38j5w25vs");
    }

    [Fact]
    public void Constructor_WithRules_SetsAllFields()
    {
        var evt = new Event("A Strange Mushroom", Rarity.Bronze,
            tooltip: "You find a mushroom.",
            selectionRule: "You can only select one card.",
            costRule: "You must pay the cost.",
            leaveRule: "You can leave.");

        evt.SelectionRule.ShouldBe("You can only select one card.");
        evt.CostRule.ShouldBe("You must pay the cost.");
        evt.LeaveRule.ShouldBe("You can leave.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidName_ThrowsArgumentException(string? name)
    {
        Should.Throw<ArgumentException>(() => new Event(name!, Rarity.Bronze));
    }

    [Fact]
    public void Options_StartsEmpty()
    {
        var evt = new Event("A Strange Mushroom", Rarity.Bronze);

        evt.Options.ShouldBeEmpty();
    }
}
```

- [ ] **Step 2: Write failing tests for EventOption**

Create `tests/BazaarOverlay.Tests/Domain/EventOptionTests.cs`:

```csharp
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using Shouldly;

namespace BazaarOverlay.Tests.Domain;

public class EventOptionTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesEventOption()
    {
        var option = new EventOption("Trade It for Something", Rarity.Bronze,
            description: "Gain a Neutral item");

        option.Name.ShouldBe("Trade It for Something");
        option.Tier.ShouldBe(Rarity.Bronze);
        option.Description.ShouldBe("Gain a Neutral item");
        option.HeroRestriction.ShouldBeNull();
    }

    [Fact]
    public void Constructor_WithHeroRestriction_SetsRestriction()
    {
        var option = new EventOption("Brew a Potion", Rarity.Bronze,
            description: "Gain a potion", heroRestriction: "Mak");

        option.HeroRestriction.ShouldBe("Mak");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidName_ThrowsArgumentException(string? name)
    {
        Should.Throw<ArgumentException>(() => new EventOption(name!, Rarity.Bronze, "desc"));
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

```bash
dotnet test tests/BazaarOverlay.Tests --filter "EventTests|EventOptionTests" --no-restore
```

Expected: FAIL — classes don't exist.

- [ ] **Step 4: Implement Event entity**

Create `src/BazaarOverlay.Domain/Entities/Event.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using BazaarOverlay.Domain.Enums;

namespace BazaarOverlay.Domain.Entities;

public class Event
{
    [Key]
    public int Id { get; private set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; private set; } = string.Empty;

    [Required]
    public Rarity Tier { get; private set; }

    [MaxLength(500)]
    public string? Tooltip { get; private set; }

    [MaxLength(200)]
    public string? SelectionRule { get; private set; }

    [MaxLength(200)]
    public string? CostRule { get; private set; }

    [MaxLength(200)]
    public string? LeaveRule { get; private set; }

    [MaxLength(50)]
    public string? BazaarDbId { get; private set; }

    public ICollection<EventOption> Options { get; private set; } = new List<EventOption>();

    private Event() { }

    public Event(string name, Rarity tier, string? tooltip = null,
        string? selectionRule = null, string? costRule = null, string? leaveRule = null,
        string? bazaarDbId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Event name cannot be empty.", nameof(name));

        Name = name.Trim();
        Tier = tier;
        Tooltip = tooltip?.Trim();
        SelectionRule = selectionRule?.Trim();
        CostRule = costRule?.Trim();
        LeaveRule = leaveRule?.Trim();
        BazaarDbId = bazaarDbId?.Trim();
    }
}
```

- [ ] **Step 5: Implement EventOption entity**

Create `src/BazaarOverlay.Domain/Entities/EventOption.cs`:

```csharp
using System.ComponentModel.DataAnnotations;
using BazaarOverlay.Domain.Enums;

namespace BazaarOverlay.Domain.Entities;

public class EventOption
{
    [Key]
    public int Id { get; private set; }

    public int EventId { get; private set; }
    public Event Event { get; private set; } = null!;

    [Required]
    [MaxLength(200)]
    public string Name { get; private set; } = string.Empty;

    [Required]
    public Rarity Tier { get; private set; }

    [MaxLength(500)]
    public string? Description { get; private set; }

    [MaxLength(50)]
    public string? HeroRestriction { get; private set; }

    private EventOption() { }

    public EventOption(string name, Rarity tier, string? description = null,
        string? heroRestriction = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Option name cannot be empty.", nameof(name));

        Name = name.Trim();
        Tier = tier;
        Description = description?.Trim();
        HeroRestriction = heroRestriction?.Trim();
    }
}
```

- [ ] **Step 6: Run tests to verify they pass**

```bash
dotnet test tests/BazaarOverlay.Tests --filter "EventTests|EventOptionTests" --no-restore
```

Expected: PASS

- [ ] **Step 7: Commit**

```bash
git add src/BazaarOverlay.Domain/Entities/Event.cs src/BazaarOverlay.Domain/Entities/EventOption.cs tests/BazaarOverlay.Tests/Domain/EventTests.cs tests/BazaarOverlay.Tests/Domain/EventOptionTests.cs
git commit -m "Create Event and EventOption domain entities"
```

---

## Task 9: Update BazaarDbContext and Repository Interfaces

**Files:**
- Modify: `src/BazaarOverlay.Infrastructure/Persistence/BazaarDbContext.cs`
- Create: `src/BazaarOverlay.Domain/Interfaces/IMerchantRepository.cs`
- Create: `src/BazaarOverlay.Domain/Interfaces/ITrainerRepository.cs`
- Create: `src/BazaarOverlay.Domain/Interfaces/IEventRepository.cs`
- Modify: `src/BazaarOverlay.Domain/Interfaces/IMonsterRepository.cs`

- [ ] **Step 1: Create IMerchantRepository**

Create `src/BazaarOverlay.Domain/Interfaces/IMerchantRepository.cs`:

```csharp
using BazaarOverlay.Domain.Entities;

namespace BazaarOverlay.Domain.Interfaces;

public interface IMerchantRepository
{
    Task<Merchant?> GetByNameAsync(string name);
    Task<IReadOnlyList<Merchant>> SearchByNameAsync(string partialName);
    Task AddAsync(Merchant merchant);
}
```

- [ ] **Step 2: Create ITrainerRepository**

Create `src/BazaarOverlay.Domain/Interfaces/ITrainerRepository.cs`:

```csharp
using BazaarOverlay.Domain.Entities;

namespace BazaarOverlay.Domain.Interfaces;

public interface ITrainerRepository
{
    Task<Trainer?> GetByNameAsync(string name);
    Task<IReadOnlyList<Trainer>> SearchByNameAsync(string partialName);
    Task AddAsync(Trainer trainer);
}
```

- [ ] **Step 3: Create IEventRepository**

Create `src/BazaarOverlay.Domain/Interfaces/IEventRepository.cs`:

```csharp
using BazaarOverlay.Domain.Entities;

namespace BazaarOverlay.Domain.Interfaces;

public interface IEventRepository
{
    Task<Event?> GetByNameAsync(string name);
    Task<IReadOnlyList<Event>> SearchByNameAsync(string partialName);
    Task AddAsync(Event evt);
}
```

- [ ] **Step 4: Update IMonsterRepository**

Replace `src/BazaarOverlay.Domain/Interfaces/IMonsterRepository.cs`:

```csharp
using BazaarOverlay.Domain.Entities;

namespace BazaarOverlay.Domain.Interfaces;

public interface IMonsterRepository
{
    Task<Monster?> GetByNameAsync(string name);
    Task<IReadOnlyList<Monster>> SearchByNameAsync(string partialName);
    Task<IReadOnlyList<Monster>> GetByDayAsync(int day);
    Task AddAsync(Monster monster);
}
```

- [ ] **Step 5: Rewrite BazaarDbContext**

Replace `src/BazaarOverlay.Infrastructure/Persistence/BazaarDbContext.cs`:

```csharp
using BazaarOverlay.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BazaarOverlay.Infrastructure.Persistence;

public class BazaarDbContext : DbContext
{
    public DbSet<Hero> Heroes => Set<Hero>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<ItemTag> ItemTags => Set<ItemTag>();
    public DbSet<ItemTierValue> ItemTierValues => Set<ItemTierValue>();
    public DbSet<ItemEnchantment> ItemEnchantments => Set<ItemEnchantment>();
    public DbSet<Enchantment> Enchantments => Set<Enchantment>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<SkillTag> SkillTags => Set<SkillTag>();
    public DbSet<SkillTierValue> SkillTierValues => Set<SkillTierValue>();
    public DbSet<Monster> Monsters => Set<Monster>();
    public DbSet<Merchant> Merchants => Set<Merchant>();
    public DbSet<Trainer> Trainers => Set<Trainer>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventOption> EventOptions => Set<EventOption>();

    public BazaarDbContext(DbContextOptions<BazaarDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Hero>(entity =>
        {
            entity.HasKey(e => e.Name);
        });

        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasMany(e => e.Heroes)
                .WithMany(h => h.Items)
                .UsingEntity("ItemHero");
            entity.HasMany(e => e.Tags)
                .WithOne(t => t.Item)
                .HasForeignKey(t => t.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.TierValues)
                .WithOne(tv => tv.Item)
                .HasForeignKey(tv => tv.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Enchantments)
                .WithOne(ie => ie.Item)
                .HasForeignKey(ie => ie.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Skill>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasMany(e => e.Heroes)
                .WithMany(h => h.Skills)
                .UsingEntity("SkillHero");
            entity.HasMany(e => e.Tags)
                .WithOne(t => t.Skill)
                .HasForeignKey(t => t.SkillId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.TierValues)
                .WithOne(tv => tv.Skill)
                .HasForeignKey(tv => tv.SkillId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Monster>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasMany(e => e.BoardItems)
                .WithMany()
                .UsingEntity("MonsterBoardItem");
            entity.HasMany(e => e.BoardSkills)
                .WithMany()
                .UsingEntity("MonsterBoardSkill");
        });

        modelBuilder.Entity<Enchantment>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
        });

        modelBuilder.Entity<Merchant>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasMany(e => e.ItemPool)
                .WithMany()
                .UsingEntity("MerchantItem");
        });

        modelBuilder.Entity<Trainer>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasMany(e => e.SkillPool)
                .WithMany()
                .UsingEntity("TrainerSkill");
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasMany(e => e.Options)
                .WithOne(o => o.Event)
                .HasForeignKey(o => o.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
```

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "Update BazaarDbContext and create repository interfaces for new entities"
```

---

## Task 10: Implement Repositories

**Files:**
- Modify: `src/BazaarOverlay.Infrastructure/Persistence/Repositories/MonsterRepository.cs`
- Create: `src/BazaarOverlay.Infrastructure/Persistence/Repositories/MerchantRepository.cs`
- Create: `src/BazaarOverlay.Infrastructure/Persistence/Repositories/TrainerRepository.cs`
- Create: `src/BazaarOverlay.Infrastructure/Persistence/Repositories/EventRepository.cs`
- Test: `tests/BazaarOverlay.Tests/Infrastructure/MonsterRepositoryTests.cs`
- Test: `tests/BazaarOverlay.Tests/Infrastructure/MerchantRepositoryTests.cs`
- Test: `tests/BazaarOverlay.Tests/Infrastructure/TrainerRepositoryTests.cs`
- Test: `tests/BazaarOverlay.Tests/Infrastructure/EventRepositoryTests.cs`

- [ ] **Step 1: Write failing test for MonsterRepository**

Replace `tests/BazaarOverlay.Tests/Infrastructure/MonsterRepositoryTests.cs`:

```csharp
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using BazaarOverlay.Infrastructure.Persistence.Repositories;
using BazaarOverlay.Tests.Infrastructure;
using Shouldly;

namespace BazaarOverlay.Tests.Infrastructure;

public class MonsterRepositoryTests : IDisposable
{
    private readonly BazaarOverlay.Infrastructure.Persistence.BazaarDbContext _context;
    private readonly MonsterRepository _repository;

    public MonsterRepositoryTests()
    {
        _context = TestDbContextFactory.Create();
        _repository = new MonsterRepository(_context);
    }

    [Fact]
    public async Task GetByNameAsync_ExistingMonster_ReturnsWithBoardItems()
    {
        var item = new Item("Banana", ItemSize.Small, Rarity.Bronze);
        _context.Items.Add(item);
        await _context.SaveChangesAsync();

        var monster = new Monster("Banannibal", Rarity.Bronze, level: 1, day: 1,
            health: 100, goldReward: 2, xpReward: 3);
        monster.BoardItems.Add(item);
        _context.Monsters.Add(monster);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByNameAsync("Banannibal");

        result.ShouldNotBeNull();
        result.Name.ShouldBe("Banannibal");
        result.BoardItems.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetByDayAsync_ReturnsMatchingMonsters()
    {
        _context.Monsters.Add(new Monster("Day1", Rarity.Bronze, level: 1, day: 1,
            health: 100, goldReward: 2, xpReward: 3));
        _context.Monsters.Add(new Monster("Day3", Rarity.Silver, level: 3, day: 3,
            health: 300, goldReward: 4, xpReward: 5));
        await _context.SaveChangesAsync();

        var result = await _repository.GetByDayAsync(2);

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Day1");
    }

    public void Dispose() => _context.Dispose();
}
```

- [ ] **Step 2: Rewrite MonsterRepository**

Replace `src/BazaarOverlay.Infrastructure/Persistence/Repositories/MonsterRepository.cs`:

```csharp
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BazaarOverlay.Infrastructure.Persistence.Repositories;

public class MonsterRepository : IMonsterRepository
{
    private readonly BazaarDbContext _context;

    public MonsterRepository(BazaarDbContext context)
    {
        _context = context;
    }

    public async Task<Monster?> GetByNameAsync(string name)
    {
        return await FullQuery()
            .FirstOrDefaultAsync(m => m.Name == name);
    }

    public async Task<IReadOnlyList<Monster>> SearchByNameAsync(string partialName)
    {
        var lower = partialName.ToLower();

        var monsters = await FullQuery()
            .Where(m => m.Name.ToLower().Contains(lower))
            .ToListAsync();

        return monsters
            .OrderBy(m => m.Name.Equals(partialName, StringComparison.OrdinalIgnoreCase) ? 0
                        : m.Name.StartsWith(partialName, StringComparison.OrdinalIgnoreCase) ? 1
                        : 2)
            .ThenBy(m => m.Name)
            .ToList();
    }

    public async Task<IReadOnlyList<Monster>> GetByDayAsync(int day)
    {
        return await FullQuery()
            .Where(m => m.Day <= day)
            .ToListAsync();
    }

    public async Task AddAsync(Monster monster)
    {
        await _context.Monsters.AddAsync(monster);
        await _context.SaveChangesAsync();
    }

    private IQueryable<Monster> FullQuery()
    {
        return _context.Monsters
            .Include(m => m.BoardItems)
            .Include(m => m.BoardSkills);
    }
}
```

- [ ] **Step 3: Write failing test for MerchantRepository**

Create `tests/BazaarOverlay.Tests/Infrastructure/MerchantRepositoryTests.cs`:

```csharp
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using BazaarOverlay.Infrastructure.Persistence.Repositories;
using Shouldly;

namespace BazaarOverlay.Tests.Infrastructure;

public class MerchantRepositoryTests : IDisposable
{
    private readonly BazaarOverlay.Infrastructure.Persistence.BazaarDbContext _context;
    private readonly MerchantRepository _repository;

    public MerchantRepositoryTests()
    {
        _context = TestDbContextFactory.Create();
        _repository = new MerchantRepository(_context);
    }

    [Fact]
    public async Task GetByNameAsync_ExistingMerchant_ReturnsWithItemPool()
    {
        var item = new Item("Big Sword", ItemSize.Large, Rarity.Silver);
        _context.Items.Add(item);
        await _context.SaveChangesAsync();

        var merchant = new Merchant("Barkun", Rarity.Silver, tooltip: "Sells items");
        merchant.ItemPool.Add(item);
        _context.Merchants.Add(merchant);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByNameAsync("Barkun");

        result.ShouldNotBeNull();
        result.ItemPool.Count.ShouldBe(1);
    }

    [Fact]
    public async Task SearchByNameAsync_PartialMatch_ReturnsResults()
    {
        _context.Merchants.Add(new Merchant("Barkun", Rarity.Silver, tooltip: "Sells items"));
        _context.Merchants.Add(new Merchant("Aero", Rarity.Bronze, tooltip: "Sells vehicles"));
        await _context.SaveChangesAsync();

        var result = await _repository.SearchByNameAsync("bar");

        result.Count.ShouldBe(1);
        result[0].Name.ShouldBe("Barkun");
    }

    public void Dispose() => _context.Dispose();
}
```

- [ ] **Step 4: Implement MerchantRepository**

Create `src/BazaarOverlay.Infrastructure/Persistence/Repositories/MerchantRepository.cs`:

```csharp
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BazaarOverlay.Infrastructure.Persistence.Repositories;

public class MerchantRepository : IMerchantRepository
{
    private readonly BazaarDbContext _context;

    public MerchantRepository(BazaarDbContext context)
    {
        _context = context;
    }

    public async Task<Merchant?> GetByNameAsync(string name)
    {
        return await _context.Merchants
            .Include(m => m.ItemPool)
            .FirstOrDefaultAsync(m => m.Name == name);
    }

    public async Task<IReadOnlyList<Merchant>> SearchByNameAsync(string partialName)
    {
        var lower = partialName.ToLower();

        var merchants = await _context.Merchants
            .Include(m => m.ItemPool)
            .Where(m => m.Name.ToLower().Contains(lower))
            .ToListAsync();

        return merchants
            .OrderBy(m => m.Name.Equals(partialName, StringComparison.OrdinalIgnoreCase) ? 0
                        : m.Name.StartsWith(partialName, StringComparison.OrdinalIgnoreCase) ? 1
                        : 2)
            .ThenBy(m => m.Name)
            .ToList();
    }

    public async Task AddAsync(Merchant merchant)
    {
        await _context.Merchants.AddAsync(merchant);
        await _context.SaveChangesAsync();
    }
}
```

- [ ] **Step 5: Write failing test for TrainerRepository**

Create `tests/BazaarOverlay.Tests/Infrastructure/TrainerRepositoryTests.cs`:

```csharp
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using BazaarOverlay.Infrastructure.Persistence.Repositories;
using Shouldly;

namespace BazaarOverlay.Tests.Infrastructure;

public class TrainerRepositoryTests : IDisposable
{
    private readonly BazaarOverlay.Infrastructure.Persistence.BazaarDbContext _context;
    private readonly TrainerRepository _repository;

    public TrainerRepositoryTests()
    {
        _context = TestDbContextFactory.Create();
        _repository = new TrainerRepository(_context);
    }

    [Fact]
    public async Task GetByNameAsync_ExistingTrainer_ReturnsWithSkillPool()
    {
        var skill = new Skill("Burning Temper", Rarity.Bronze);
        _context.Skills.Add(skill);
        await _context.SaveChangesAsync();

        var trainer = new Trainer("Adira", Rarity.Diamond, tooltip: "Teaches skills");
        trainer.SkillPool.Add(skill);
        _context.Trainers.Add(trainer);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByNameAsync("Adira");

        result.ShouldNotBeNull();
        result.SkillPool.Count.ShouldBe(1);
    }

    public void Dispose() => _context.Dispose();
}
```

- [ ] **Step 6: Implement TrainerRepository**

Create `src/BazaarOverlay.Infrastructure/Persistence/Repositories/TrainerRepository.cs`:

```csharp
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BazaarOverlay.Infrastructure.Persistence.Repositories;

public class TrainerRepository : ITrainerRepository
{
    private readonly BazaarDbContext _context;

    public TrainerRepository(BazaarDbContext context)
    {
        _context = context;
    }

    public async Task<Trainer?> GetByNameAsync(string name)
    {
        return await _context.Trainers
            .Include(t => t.SkillPool)
            .FirstOrDefaultAsync(t => t.Name == name);
    }

    public async Task<IReadOnlyList<Trainer>> SearchByNameAsync(string partialName)
    {
        var lower = partialName.ToLower();

        var trainers = await _context.Trainers
            .Include(t => t.SkillPool)
            .Where(t => t.Name.ToLower().Contains(lower))
            .ToListAsync();

        return trainers
            .OrderBy(t => t.Name.Equals(partialName, StringComparison.OrdinalIgnoreCase) ? 0
                        : t.Name.StartsWith(partialName, StringComparison.OrdinalIgnoreCase) ? 1
                        : 2)
            .ThenBy(t => t.Name)
            .ToList();
    }

    public async Task AddAsync(Trainer trainer)
    {
        await _context.Trainers.AddAsync(trainer);
        await _context.SaveChangesAsync();
    }
}
```

- [ ] **Step 7: Write failing test for EventRepository**

Create `tests/BazaarOverlay.Tests/Infrastructure/EventRepositoryTests.cs`:

```csharp
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using BazaarOverlay.Infrastructure.Persistence.Repositories;
using Shouldly;

namespace BazaarOverlay.Tests.Infrastructure;

public class EventRepositoryTests : IDisposable
{
    private readonly BazaarOverlay.Infrastructure.Persistence.BazaarDbContext _context;
    private readonly EventRepository _repository;

    public EventRepositoryTests()
    {
        _context = TestDbContextFactory.Create();
        _repository = new EventRepository(_context);
    }

    [Fact]
    public async Task GetByNameAsync_ExistingEvent_ReturnsWithOptions()
    {
        var evt = new Event("A Strange Mushroom", Rarity.Bronze, tooltip: "You find a mushroom.");
        evt.Options.Add(new EventOption("Trade It", Rarity.Bronze, description: "Gain a Neutral item"));
        evt.Options.Add(new EventOption("Sell It", Rarity.Bronze, description: "Gain 4 Gold."));
        _context.Events.Add(evt);
        await _context.SaveChangesAsync();

        var result = await _repository.GetByNameAsync("A Strange Mushroom");

        result.ShouldNotBeNull();
        result.Options.Count.ShouldBe(2);
    }

    public void Dispose() => _context.Dispose();
}
```

- [ ] **Step 8: Implement EventRepository**

Create `src/BazaarOverlay.Infrastructure/Persistence/Repositories/EventRepository.cs`:

```csharp
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BazaarOverlay.Infrastructure.Persistence.Repositories;

public class EventRepository : IEventRepository
{
    private readonly BazaarDbContext _context;

    public EventRepository(BazaarDbContext context)
    {
        _context = context;
    }

    public async Task<Event?> GetByNameAsync(string name)
    {
        return await _context.Events
            .Include(e => e.Options)
            .FirstOrDefaultAsync(e => e.Name == name);
    }

    public async Task<IReadOnlyList<Event>> SearchByNameAsync(string partialName)
    {
        var lower = partialName.ToLower();

        var events = await _context.Events
            .Include(e => e.Options)
            .Where(e => e.Name.ToLower().Contains(lower))
            .ToListAsync();

        return events
            .OrderBy(e => e.Name.Equals(partialName, StringComparison.OrdinalIgnoreCase) ? 0
                        : e.Name.StartsWith(partialName, StringComparison.OrdinalIgnoreCase) ? 1
                        : 2)
            .ThenBy(e => e.Name)
            .ToList();
    }

    public async Task AddAsync(Event evt)
    {
        await _context.Events.AddAsync(evt);
        await _context.SaveChangesAsync();
    }
}
```

- [ ] **Step 9: Run all repository tests**

```bash
dotnet test tests/BazaarOverlay.Tests --filter "RepositoryTests" --no-restore
```

Expected: PASS

- [ ] **Step 10: Commit**

```bash
git add -A
git commit -m "Implement Monster, Merchant, Trainer, Event repositories with tests"
```

---

## Task 11: Update Application DTOs and Services

**Files:**
- Modify: `src/BazaarOverlay.Application/DTOs/ItemInfoResult.cs`
- Modify: `src/BazaarOverlay.Application/DTOs/SkillInfoResult.cs`
- Create: `src/BazaarOverlay.Application/DTOs/MonsterResult.cs`
- Create: `src/BazaarOverlay.Application/DTOs/MerchantResult.cs`
- Create: `src/BazaarOverlay.Application/DTOs/TrainerResult.cs`
- Create: `src/BazaarOverlay.Application/DTOs/EventResult.cs`
- Create: `src/BazaarOverlay.Application/Interfaces/IMonsterService.cs`
- Create: `src/BazaarOverlay.Application/Interfaces/IMerchantService.cs`
- Create: `src/BazaarOverlay.Application/Interfaces/ITrainerService.cs`
- Create: `src/BazaarOverlay.Application/Interfaces/IEventService.cs`
- Modify: `src/BazaarOverlay.Application/Interfaces/IDataImportService.cs`
- Modify: `src/BazaarOverlay.Application/Services/ItemInfoService.cs`
- Modify: `src/BazaarOverlay.Application/Services/SkillInfoService.cs`
- Create: `src/BazaarOverlay.Application/Services/MonsterService.cs`
- Create: `src/BazaarOverlay.Application/Services/MerchantService.cs`
- Create: `src/BazaarOverlay.Application/Services/TrainerService.cs`
- Create: `src/BazaarOverlay.Application/Services/EventService.cs`
- Test: update/create service test files

- [ ] **Step 1: Update ItemInfoResult DTO**

Replace `src/BazaarOverlay.Application/DTOs/ItemInfoResult.cs`:

```csharp
using BazaarOverlay.Domain.Enums;

namespace BazaarOverlay.Application.DTOs;

public record ItemInfoResult(
    string Name,
    ItemSize Size,
    decimal? Cooldown,
    Rarity MinimumRarity,
    string? Cost,
    string? Value,
    string? Description,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Heroes,
    IReadOnlyList<TierValueResult> TierValues,
    IReadOnlyList<EnchantmentResult> Enchantments
);

public record TierValueResult(Rarity Rarity, string EffectDescription);

public record EnchantmentResult(string EnchantmentName, string EffectDescription);
```

- [ ] **Step 2: Update SkillInfoResult DTO**

Replace `src/BazaarOverlay.Application/DTOs/SkillInfoResult.cs`:

```csharp
using BazaarOverlay.Domain.Enums;

namespace BazaarOverlay.Application.DTOs;

public record SkillInfoResult(
    string Name,
    Rarity MinimumRarity,
    string? Cost,
    string? Description,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Heroes,
    IReadOnlyList<TierValueResult> TierValues
);
```

- [ ] **Step 3: Create new DTOs**

Create `src/BazaarOverlay.Application/DTOs/MonsterResult.cs`:

```csharp
using BazaarOverlay.Domain.Enums;

namespace BazaarOverlay.Application.DTOs;

public record MonsterResult(
    string Name,
    Rarity Tier,
    int Level,
    int Day,
    int Health,
    int GoldReward,
    int XpReward,
    IReadOnlyList<string> BoardItemNames,
    IReadOnlyList<string> BoardSkillNames
);
```

Create `src/BazaarOverlay.Application/DTOs/MerchantResult.cs`:

```csharp
using BazaarOverlay.Domain.Enums;

namespace BazaarOverlay.Application.DTOs;

public record MerchantResult(
    string Name,
    Rarity Tier,
    string? Tooltip,
    string? SelectionRule,
    string? CostRule,
    string? LeaveRule,
    int? RerollCount,
    int? RerollCost,
    IReadOnlyList<string> ItemPoolNames
);
```

Create `src/BazaarOverlay.Application/DTOs/TrainerResult.cs`:

```csharp
using BazaarOverlay.Domain.Enums;

namespace BazaarOverlay.Application.DTOs;

public record TrainerResult(
    string Name,
    Rarity Tier,
    string? Tooltip,
    string? SelectionRule,
    string? CostRule,
    string? LeaveRule,
    int? RerollCount,
    int? RerollCost,
    IReadOnlyList<string> SkillPoolNames
);
```

Create `src/BazaarOverlay.Application/DTOs/EventResult.cs`:

```csharp
using BazaarOverlay.Domain.Enums;

namespace BazaarOverlay.Application.DTOs;

public record EventResult(
    string Name,
    Rarity Tier,
    string? Tooltip,
    string? SelectionRule,
    string? CostRule,
    string? LeaveRule,
    IReadOnlyList<EventOptionResult> Options
);

public record EventOptionResult(
    string Name,
    Rarity Tier,
    string? Description,
    string? HeroRestriction
);
```

- [ ] **Step 4: Create new service interfaces**

Create `src/BazaarOverlay.Application/Interfaces/IMonsterService.cs`:

```csharp
using BazaarOverlay.Application.DTOs;

namespace BazaarOverlay.Application.Interfaces;

public interface IMonsterService
{
    Task<MonsterResult?> GetMonsterAsync(string name);
    Task<IReadOnlyList<MonsterResult>> SearchMonstersAsync(string partialName);
    Task<IReadOnlyList<MonsterResult>> GetMonstersByDayAsync(int day);
}
```

Create `src/BazaarOverlay.Application/Interfaces/IMerchantService.cs`:

```csharp
using BazaarOverlay.Application.DTOs;

namespace BazaarOverlay.Application.Interfaces;

public interface IMerchantService
{
    Task<MerchantResult?> GetMerchantAsync(string name);
    Task<IReadOnlyList<MerchantResult>> SearchMerchantsAsync(string partialName);
}
```

Create `src/BazaarOverlay.Application/Interfaces/ITrainerService.cs`:

```csharp
using BazaarOverlay.Application.DTOs;

namespace BazaarOverlay.Application.Interfaces;

public interface ITrainerService
{
    Task<TrainerResult?> GetTrainerAsync(string name);
    Task<IReadOnlyList<TrainerResult>> SearchTrainersAsync(string partialName);
}
```

Create `src/BazaarOverlay.Application/Interfaces/IEventService.cs`:

```csharp
using BazaarOverlay.Application.DTOs;

namespace BazaarOverlay.Application.Interfaces;

public interface IEventService
{
    Task<EventResult?> GetEventAsync(string name);
    Task<IReadOnlyList<EventResult>> SearchEventsAsync(string partialName);
}
```

- [ ] **Step 5: Simplify IDataImportService**

Replace `src/BazaarOverlay.Application/Interfaces/IDataImportService.cs`:

```csharp
namespace BazaarOverlay.Application.Interfaces;

public interface IDataImportService
{
    Task ImportAllAsync(IProgress<string>? progress = null);
}
```

- [ ] **Step 6: Update ItemInfoService to map new fields**

Replace `src/BazaarOverlay.Application/Services/ItemInfoService.cs`:

```csharp
using BazaarOverlay.Application.DTOs;
using BazaarOverlay.Application.Interfaces;
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace BazaarOverlay.Application.Services;

public class ItemInfoService : IItemInfoService
{
    private readonly IItemRepository _itemRepository;
    private readonly ILogger<ItemInfoService> _logger;

    public ItemInfoService(IItemRepository itemRepository, ILogger<ItemInfoService> logger)
    {
        _itemRepository = itemRepository;
        _logger = logger;
    }

    public async Task<ItemInfoResult?> GetItemInfoAsync(string itemName)
    {
        _logger.LogInformation("Looking up item: {ItemName}", itemName);
        var item = await _itemRepository.GetByNameAsync(itemName);
        if (item is null)
        {
            _logger.LogWarning("Item not found: {ItemName}", itemName);
            return null;
        }
        return MapToResult(item);
    }

    public async Task<IReadOnlyList<ItemInfoResult>> SearchItemsAsync(string partialName)
    {
        _logger.LogInformation("Searching items: {Query}", partialName);
        var items = await _itemRepository.SearchByNameAsync(partialName);
        return items.Select(MapToResult).ToList();
    }

    private static ItemInfoResult MapToResult(Item item)
    {
        return new ItemInfoResult(
            item.Name,
            item.Size,
            item.Cooldown,
            item.MinimumRarity,
            item.Cost,
            item.Value,
            item.Description,
            item.Tags.Select(t => t.Tag).ToList(),
            item.Heroes.Select(h => h.Name).ToList(),
            item.TierValues
                .OrderBy(tv => tv.Rarity)
                .Select(tv => new TierValueResult(tv.Rarity, tv.EffectDescription))
                .ToList(),
            item.Enchantments
                .Select(ie => new EnchantmentResult(ie.Enchantment.Name, ie.EffectDescription))
                .ToList()
        );
    }
}
```

- [ ] **Step 7: Update SkillInfoService to map new fields**

Replace `src/BazaarOverlay.Application/Services/SkillInfoService.cs`:

```csharp
using BazaarOverlay.Application.DTOs;
using BazaarOverlay.Application.Interfaces;
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace BazaarOverlay.Application.Services;

public class SkillInfoService : ISkillInfoService
{
    private readonly ISkillRepository _skillRepository;
    private readonly ILogger<SkillInfoService> _logger;

    public SkillInfoService(ISkillRepository skillRepository, ILogger<SkillInfoService> logger)
    {
        _skillRepository = skillRepository;
        _logger = logger;
    }

    public async Task<SkillInfoResult?> GetSkillInfoAsync(string skillName)
    {
        _logger.LogInformation("Looking up skill: {SkillName}", skillName);
        var skill = await _skillRepository.GetByNameAsync(skillName);
        if (skill is null)
        {
            _logger.LogWarning("Skill not found: {SkillName}", skillName);
            return null;
        }
        return MapToResult(skill);
    }

    public async Task<IReadOnlyList<SkillInfoResult>> SearchSkillsAsync(string partialName)
    {
        _logger.LogInformation("Searching skills: {Query}", partialName);
        var skills = await _skillRepository.SearchByNameAsync(partialName);
        return skills.Select(MapToResult).ToList();
    }

    private static SkillInfoResult MapToResult(Skill skill)
    {
        return new SkillInfoResult(
            skill.Name,
            skill.MinimumRarity,
            skill.Cost,
            skill.Description,
            skill.Tags.Select(t => t.Tag).ToList(),
            skill.Heroes.Select(h => h.Name).ToList(),
            skill.TierValues
                .OrderBy(tv => tv.Rarity)
                .Select(tv => new TierValueResult(tv.Rarity, tv.EffectDescription))
                .ToList()
        );
    }
}
```

- [ ] **Step 8: Create MonsterService**

Create `src/BazaarOverlay.Application/Services/MonsterService.cs`:

```csharp
using BazaarOverlay.Application.DTOs;
using BazaarOverlay.Application.Interfaces;
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace BazaarOverlay.Application.Services;

public class MonsterService : IMonsterService
{
    private readonly IMonsterRepository _monsterRepository;
    private readonly ILogger<MonsterService> _logger;

    public MonsterService(IMonsterRepository monsterRepository, ILogger<MonsterService> logger)
    {
        _monsterRepository = monsterRepository;
        _logger = logger;
    }

    public async Task<MonsterResult?> GetMonsterAsync(string name)
    {
        var monster = await _monsterRepository.GetByNameAsync(name);
        return monster is null ? null : MapToResult(monster);
    }

    public async Task<IReadOnlyList<MonsterResult>> SearchMonstersAsync(string partialName)
    {
        _logger.LogInformation("Searching monsters: {Query}", partialName);
        var monsters = await _monsterRepository.SearchByNameAsync(partialName);
        return monsters.Select(MapToResult).ToList();
    }

    public async Task<IReadOnlyList<MonsterResult>> GetMonstersByDayAsync(int day)
    {
        var monsters = await _monsterRepository.GetByDayAsync(day);
        return monsters.Select(MapToResult).ToList();
    }

    private static MonsterResult MapToResult(Monster monster)
    {
        return new MonsterResult(
            monster.Name, monster.Tier, monster.Level, monster.Day,
            monster.Health, monster.GoldReward, monster.XpReward,
            monster.BoardItems.Select(i => i.Name).ToList(),
            monster.BoardSkills.Select(s => s.Name).ToList()
        );
    }
}
```

- [ ] **Step 9: Create MerchantService, TrainerService, EventService**

Create `src/BazaarOverlay.Application/Services/MerchantService.cs`:

```csharp
using BazaarOverlay.Application.DTOs;
using BazaarOverlay.Application.Interfaces;
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace BazaarOverlay.Application.Services;

public class MerchantService : IMerchantService
{
    private readonly IMerchantRepository _merchantRepository;
    private readonly ILogger<MerchantService> _logger;

    public MerchantService(IMerchantRepository merchantRepository, ILogger<MerchantService> logger)
    {
        _merchantRepository = merchantRepository;
        _logger = logger;
    }

    public async Task<MerchantResult?> GetMerchantAsync(string name)
    {
        var merchant = await _merchantRepository.GetByNameAsync(name);
        return merchant is null ? null : MapToResult(merchant);
    }

    public async Task<IReadOnlyList<MerchantResult>> SearchMerchantsAsync(string partialName)
    {
        _logger.LogInformation("Searching merchants: {Query}", partialName);
        var merchants = await _merchantRepository.SearchByNameAsync(partialName);
        return merchants.Select(MapToResult).ToList();
    }

    private static MerchantResult MapToResult(Merchant merchant)
    {
        return new MerchantResult(
            merchant.Name, merchant.Tier, merchant.Tooltip,
            merchant.SelectionRule, merchant.CostRule, merchant.LeaveRule,
            merchant.RerollCount, merchant.RerollCost,
            merchant.ItemPool.Select(i => i.Name).ToList()
        );
    }
}
```

Create `src/BazaarOverlay.Application/Services/TrainerService.cs`:

```csharp
using BazaarOverlay.Application.DTOs;
using BazaarOverlay.Application.Interfaces;
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace BazaarOverlay.Application.Services;

public class TrainerService : ITrainerService
{
    private readonly ITrainerRepository _trainerRepository;
    private readonly ILogger<TrainerService> _logger;

    public TrainerService(ITrainerRepository trainerRepository, ILogger<TrainerService> logger)
    {
        _trainerRepository = trainerRepository;
        _logger = logger;
    }

    public async Task<TrainerResult?> GetTrainerAsync(string name)
    {
        var trainer = await _trainerRepository.GetByNameAsync(name);
        return trainer is null ? null : MapToResult(trainer);
    }

    public async Task<IReadOnlyList<TrainerResult>> SearchTrainersAsync(string partialName)
    {
        _logger.LogInformation("Searching trainers: {Query}", partialName);
        var trainers = await _trainerRepository.SearchByNameAsync(partialName);
        return trainers.Select(MapToResult).ToList();
    }

    private static TrainerResult MapToResult(Trainer trainer)
    {
        return new TrainerResult(
            trainer.Name, trainer.Tier, trainer.Tooltip,
            trainer.SelectionRule, trainer.CostRule, trainer.LeaveRule,
            trainer.RerollCount, trainer.RerollCost,
            trainer.SkillPool.Select(s => s.Name).ToList()
        );
    }
}
```

Create `src/BazaarOverlay.Application/Services/EventService.cs`:

```csharp
using BazaarOverlay.Application.DTOs;
using BazaarOverlay.Application.Interfaces;
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace BazaarOverlay.Application.Services;

public class EventService : IEventService
{
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<EventService> _logger;

    public EventService(IEventRepository eventRepository, ILogger<EventService> logger)
    {
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task<EventResult?> GetEventAsync(string name)
    {
        var evt = await _eventRepository.GetByNameAsync(name);
        return evt is null ? null : MapToResult(evt);
    }

    public async Task<IReadOnlyList<EventResult>> SearchEventsAsync(string partialName)
    {
        _logger.LogInformation("Searching events: {Query}", partialName);
        var events = await _eventRepository.SearchByNameAsync(partialName);
        return events.Select(MapToResult).ToList();
    }

    private static EventResult MapToResult(Event evt)
    {
        return new EventResult(
            evt.Name, evt.Tier, evt.Tooltip,
            evt.SelectionRule, evt.CostRule, evt.LeaveRule,
            evt.Options.Select(o => new EventOptionResult(
                o.Name, o.Tier, o.Description, o.HeroRestriction
            )).ToList()
        );
    }
}
```

- [ ] **Step 10: Write service tests**

Create `tests/BazaarOverlay.Tests/Application/MonsterServiceTests.cs`:

```csharp
using BazaarOverlay.Application.Services;
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using BazaarOverlay.Domain.Interfaces;
using BazaarOverlay.Tests.Helpers;
using NSubstitute;
using Shouldly;

namespace BazaarOverlay.Tests.Application;

public class MonsterServiceTests
{
    private readonly IMonsterRepository _monsterRepository = Substitute.For<IMonsterRepository>();
    private readonly MonsterService _service;

    public MonsterServiceTests()
    {
        _service = new MonsterService(_monsterRepository, new TestLogger<MonsterService>());
    }

    [Fact]
    public async Task GetMonsterAsync_ExistingMonster_ReturnsMapped()
    {
        var monster = new Monster("Banannibal", Rarity.Bronze, level: 1, day: 1,
            health: 100, goldReward: 2, xpReward: 3);
        _monsterRepository.GetByNameAsync("Banannibal").Returns(monster);

        var result = await _service.GetMonsterAsync("Banannibal");

        result.ShouldNotBeNull();
        result.Name.ShouldBe("Banannibal");
        result.Health.ShouldBe(100);
        result.GoldReward.ShouldBe(2);
    }

    [Fact]
    public async Task GetMonsterAsync_NonExistent_ReturnsNull()
    {
        _monsterRepository.GetByNameAsync("Unknown").Returns((Monster?)null);

        var result = await _service.GetMonsterAsync("Unknown");

        result.ShouldBeNull();
    }
}
```

Create `tests/BazaarOverlay.Tests/Application/MerchantServiceTests.cs`:

```csharp
using BazaarOverlay.Application.Services;
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using BazaarOverlay.Domain.Interfaces;
using BazaarOverlay.Tests.Helpers;
using NSubstitute;
using Shouldly;

namespace BazaarOverlay.Tests.Application;

public class MerchantServiceTests
{
    private readonly IMerchantRepository _merchantRepository = Substitute.For<IMerchantRepository>();
    private readonly MerchantService _service;

    public MerchantServiceTests()
    {
        _service = new MerchantService(_merchantRepository, new TestLogger<MerchantService>());
    }

    [Fact]
    public async Task GetMerchantAsync_ExistingMerchant_ReturnsMapped()
    {
        var merchant = new Merchant("Barkun", Rarity.Silver, tooltip: "Sells items");
        _merchantRepository.GetByNameAsync("Barkun").Returns(merchant);

        var result = await _service.GetMerchantAsync("Barkun");

        result.ShouldNotBeNull();
        result.Name.ShouldBe("Barkun");
        result.Tooltip.ShouldBe("Sells items");
    }
}
```

Create `tests/BazaarOverlay.Tests/Application/TrainerServiceTests.cs`:

```csharp
using BazaarOverlay.Application.Services;
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using BazaarOverlay.Domain.Interfaces;
using BazaarOverlay.Tests.Helpers;
using NSubstitute;
using Shouldly;

namespace BazaarOverlay.Tests.Application;

public class TrainerServiceTests
{
    private readonly ITrainerRepository _trainerRepository = Substitute.For<ITrainerRepository>();
    private readonly TrainerService _service;

    public TrainerServiceTests()
    {
        _service = new TrainerService(_trainerRepository, new TestLogger<TrainerService>());
    }

    [Fact]
    public async Task GetTrainerAsync_ExistingTrainer_ReturnsMapped()
    {
        var trainer = new Trainer("Adira", Rarity.Diamond, tooltip: "Teaches skills");
        _trainerRepository.GetByNameAsync("Adira").Returns(trainer);

        var result = await _service.GetTrainerAsync("Adira");

        result.ShouldNotBeNull();
        result.Name.ShouldBe("Adira");
    }
}
```

Create `tests/BazaarOverlay.Tests/Application/EventServiceTests.cs`:

```csharp
using BazaarOverlay.Application.Services;
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using BazaarOverlay.Domain.Interfaces;
using BazaarOverlay.Tests.Helpers;
using NSubstitute;
using Shouldly;

namespace BazaarOverlay.Tests.Application;

public class EventServiceTests
{
    private readonly IEventRepository _eventRepository = Substitute.For<IEventRepository>();
    private readonly EventService _service;

    public EventServiceTests()
    {
        _service = new EventService(_eventRepository, new TestLogger<EventService>());
    }

    [Fact]
    public async Task GetEventAsync_ExistingEvent_ReturnsMappedWithOptions()
    {
        var evt = new Event("A Strange Mushroom", Rarity.Bronze, tooltip: "Find a mushroom.");
        evt.Options.Add(new EventOption("Trade It", Rarity.Bronze, description: "Gain a Neutral item"));
        _eventRepository.GetByNameAsync("A Strange Mushroom").Returns(evt);

        var result = await _service.GetEventAsync("A Strange Mushroom");

        result.ShouldNotBeNull();
        result.Options.Count.ShouldBe(1);
        result.Options[0].Name.ShouldBe("Trade It");
    }
}
```

- [ ] **Step 11: Update existing ItemInfoService and SkillInfoService tests**

Update `tests/BazaarOverlay.Tests/Application/ItemInfoServiceTests.cs` to pass the new `Cost`, `Value`, `Description` fields in the DTO assertions. Check what the current test expects and update the `MapToResult` assertions to include the new nullable fields.

Update `tests/BazaarOverlay.Tests/Application/SkillInfoServiceTests.cs` similarly for `Cost` and `Description`.

- [ ] **Step 12: Run all tests**

```bash
dotnet test tests/BazaarOverlay.Tests --no-restore
```

Expected: PASS

- [ ] **Step 13: Commit**

```bash
git add -A
git commit -m "Add application DTOs and services for Monster, Merchant, Trainer, Event"
```

---

## Task 12: Add PuppeteerSharp and Create Scraping Infrastructure

**Files:**
- Modify: `src/BazaarOverlay.Infrastructure/BazaarOverlay.Infrastructure.csproj`
- Create: `src/BazaarOverlay.Infrastructure/Scraping/IBazaarDbScraper.cs`
- Create: `src/BazaarOverlay.Infrastructure/Scraping/ScraperBase.cs`

- [ ] **Step 1: Add PuppeteerSharp NuGet package**

```bash
dotnet add src/BazaarOverlay.Infrastructure/BazaarOverlay.Infrastructure.csproj package PuppeteerSharp
```

- [ ] **Step 2: Create IBazaarDbScraper interface**

Create `src/BazaarOverlay.Infrastructure/Scraping/IBazaarDbScraper.cs`:

```csharp
using BazaarOverlay.Domain.Entities;

namespace BazaarOverlay.Infrastructure.Scraping;

public interface IBazaarDbScraper : IDisposable
{
    Task<IReadOnlyList<Item>> ScrapeItemsAsync(
        IReadOnlyDictionary<string, Hero> heroes,
        IReadOnlyDictionary<string, Enchantment> enchantments,
        IProgress<string>? progress = null);

    Task<IReadOnlyList<Skill>> ScrapeSkillsAsync(
        IReadOnlyDictionary<string, Hero> heroes,
        IProgress<string>? progress = null);

    Task<IReadOnlyList<Monster>> ScrapeMonstersAsync(
        IProgress<string>? progress = null);

    Task<IReadOnlyList<Merchant>> ScrapeMerchantsAsync(
        IProgress<string>? progress = null);

    Task<IReadOnlyList<Trainer>> ScrapeTrainersAsync(
        IProgress<string>? progress = null);

    Task<IReadOnlyList<Event>> ScrapeEventsAsync(
        IProgress<string>? progress = null);

    Task InitializeAsync(IProgress<string>? progress = null);
}
```

- [ ] **Step 3: Create ScraperBase**

Create `src/BazaarOverlay.Infrastructure/Scraping/ScraperBase.cs`:

```csharp
using Microsoft.Extensions.Logging;
using PuppeteerSharp;

namespace BazaarOverlay.Infrastructure.Scraping;

public abstract class ScraperBase
{
    protected readonly ILogger Logger;
    private const int DelayBetweenPages = 500;
    private const int PageTimeout = 30_000;

    protected ScraperBase(ILogger logger)
    {
        Logger = logger;
    }

    protected async Task<List<string>> CollectCardLinksAsync(IPage page, string category)
    {
        var url = $"https://bazaardb.gg/search?c={category}";
        await page.GoToAsync(url, new NavigationOptions { WaitUntil = [WaitUntilNavigation.NetworkIdle] });
        await page.WaitForSelectorAsync("a[href^='/card/']",
            new WaitForSelectorOptions { Timeout = PageTimeout });

        // Click "Load more" until all cards are visible
        while (true)
        {
            var loadMore = await page.QuerySelectorAsync("text/Load more");
            if (loadMore is null)
                break;

            try
            {
                await loadMore.ClickAsync();
                await Task.Delay(1000);
            }
            catch
            {
                break;
            }
        }

        // Collect all unique card links
        var links = await page.EvaluateExpressionAsync<string[]>(
            "[...new Set([...document.querySelectorAll('a[href^=\"/card/\"]')].map(a => a.getAttribute('href')))]");

        Logger.LogInformation("Found {Count} card links for {Category}", links.Length, category);
        return links.ToList();
    }

    protected async Task NavigateToDetailPageAsync(IPage page, string href)
    {
        var url = $"https://bazaardb.gg{href}";
        await page.GoToAsync(url, new NavigationOptions { WaitUntil = [WaitUntilNavigation.NetworkIdle] });
        await Task.Delay(DelayBetweenPages);
    }

    protected static string? ExtractText(string? raw)
    {
        return string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();
    }
}
```

- [ ] **Step 4: Verify build**

```bash
dotnet build src/BazaarOverlay.Infrastructure --no-restore
```

Expected: SUCCESS

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "Add PuppeteerSharp dependency and scraping base infrastructure"
```

---

## Task 13: Create Category Scrapers

**Files:**
- Create: `src/BazaarOverlay.Infrastructure/Scraping/ItemScraper.cs`
- Create: `src/BazaarOverlay.Infrastructure/Scraping/SkillScraper.cs`
- Create: `src/BazaarOverlay.Infrastructure/Scraping/MonsterScraper.cs`
- Create: `src/BazaarOverlay.Infrastructure/Scraping/MerchantScraper.cs`
- Create: `src/BazaarOverlay.Infrastructure/Scraping/TrainerScraper.cs`
- Create: `src/BazaarOverlay.Infrastructure/Scraping/EventScraper.cs`

Each scraper navigates to detail pages and extracts data from the DOM using JavaScript evaluation. The exact CSS selectors and extraction logic will need to be refined against the live site during implementation. The patterns below show the structure — adjust selectors as needed based on actual DOM.

**Important:** bazaardb.gg uses semantic HTML with headings, links, and text content. The scrapers use `page.EvaluateExpressionAsync<T>()` to run JS in the browser context and return structured data.

- [ ] **Step 1: Create ItemScraper**

Create `src/BazaarOverlay.Infrastructure/Scraping/ItemScraper.cs`:

```csharp
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;

namespace BazaarOverlay.Infrastructure.Scraping;

public class ItemScraper : ScraperBase
{
    public ItemScraper(ILogger logger) : base(logger) { }

    public async Task<List<Item>> ScrapeAllAsync(IPage page,
        IReadOnlyDictionary<string, Hero> heroes,
        IReadOnlyDictionary<string, Enchantment> enchantments,
        IProgress<string>? progress = null)
    {
        var links = await CollectCardLinksAsync(page, "items");
        var items = new List<Item>();

        for (var i = 0; i < links.Count; i++)
        {
            progress?.Report($"Scraping items... ({i + 1}/{links.Count})");
            try
            {
                await NavigateToDetailPageAsync(page, links[i]);
                var item = await ParseItemPageAsync(page, links[i], heroes, enchantments);
                if (item is not null)
                    items.Add(item);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to scrape item at {Link}", links[i]);
            }
        }

        Logger.LogInformation("Scraped {Count} items", items.Count);
        return items;
    }

    private async Task<Item?> ParseItemPageAsync(IPage page, string href,
        IReadOnlyDictionary<string, Hero> heroes,
        IReadOnlyDictionary<string, Enchantment> enchantments)
    {
        // Extract the bazaardb ID from the URL: /card/{id}/{slug}
        var parts = href.Split('/');
        var bazaarDbId = parts.Length >= 3 ? parts[2] : null;

        // Extract data from the page via JS evaluation
        var data = await page.EvaluateFunctionAsync<ItemPageData>(@"() => {
            const getText = (sel) => document.querySelector(sel)?.textContent?.trim() ?? null;
            const getAll = (sel) => [...document.querySelectorAll(sel)].map(e => e.textContent.trim());

            // Name from H1
            const name = document.querySelector('h1')?.textContent?.replace('Copy link', '')?.trim();

            // Tags from sidebar links
            const tagLinks = [...document.querySelectorAll('a[href*=""search?q=t%3A""]')];
            const tags = tagLinks.map(a => a.textContent.trim());

            // Types from sidebar links to search?q=t%3A{type}
            const typeLinks = [...document.querySelectorAll('a[href*=""search?q=t%3A""]')];

            // Hero badges (3-letter codes)
            const heroBadges = [...document.querySelectorAll('a[href*=""search?q=""]')]
                .map(a => a.textContent.trim())
                .filter(t => t.length === 3 && t === t.toUpperCase());

            // Description from sidebar tooltip text (the main card description)
            // This is typically inside the card panel, look for styled paragraph text
            const descEl = document.querySelector('.card-tooltip, [class*=""tooltip""]');
            const description = descEl?.textContent?.trim() ?? null;

            // Enchantments section
            const enchantEls = [...document.querySelectorAll('a[href*=""#engine-""]')];
            const enchantments = enchantEls.map(el => {
                const name = el.textContent.trim();
                const parent = el.closest('li, div');
                const tooltip = parent?.querySelector('[class*=""tooltip""], p, span:not(:first-child)')?.textContent?.trim() ?? '';
                return { name, tooltip };
            });

            return { name, tags, heroBadges, description, enchantments };
        }");

        if (string.IsNullOrWhiteSpace(data?.Name))
            return null;

        var size = ParseSizeFromTags(data.Tags);
        var rarity = ParseRarityFromTags(data.Tags);

        var item = new Item(data.Name, size, rarity, bazaarDbId: bazaarDbId);

        // Add heroes
        var heroAbbrevMap = heroes.Values.ToDictionary(h => h.Abbreviation, h => h, StringComparer.OrdinalIgnoreCase);
        if (data.HeroBadges?.Any() == true)
        {
            foreach (var badge in data.HeroBadges)
            {
                if (heroAbbrevMap.TryGetValue(badge, out var hero))
                    item.Heroes.Add(hero);
            }
        }
        else if (heroes.TryGetValue("Neutral", out var neutral))
        {
            item.Heroes.Add(neutral);
        }

        // Add tags (excluding size and tier tags)
        var skipTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "Small", "Medium", "Large", "Bronze", "Silver", "Gold", "Diamond", "Legendary",
              "Bronze+", "Silver+", "Gold+", "Diamond+", "Item" };
        foreach (var tag in data.Tags ?? [])
        {
            if (!skipTags.Contains(tag) && tag.Length > 3)
                item.Tags.Add(new ItemTag(tag));
        }

        // Add enchantments
        if (data.Enchantments is not null)
        {
            foreach (var enc in data.Enchantments)
            {
                var encName = enc.Name?.Replace("Enchantment:", "").Trim();
                if (encName is not null && enchantments.TryGetValue(encName, out var enchantment))
                {
                    var effectDesc = enc.Tooltip ?? enchantment.GlobalDescription;
                    item.Enchantments.Add(new ItemEnchantment(enchantment, effectDesc));
                }
            }
        }

        return item;
    }

    private static ItemSize ParseSizeFromTags(IEnumerable<string>? tags)
    {
        if (tags is null) return ItemSize.Small;
        foreach (var tag in tags)
        {
            if (tag.Equals("Large", StringComparison.OrdinalIgnoreCase)) return ItemSize.Large;
            if (tag.Equals("Medium", StringComparison.OrdinalIgnoreCase)) return ItemSize.Medium;
            if (tag.Equals("Small", StringComparison.OrdinalIgnoreCase)) return ItemSize.Small;
        }
        return ItemSize.Small;
    }

    private static Rarity ParseRarityFromTags(IEnumerable<string>? tags)
    {
        if (tags is null) return Rarity.Bronze;
        foreach (var tag in tags)
        {
            var clean = tag.TrimEnd('+');
            if (Enum.TryParse<Rarity>(clean, true, out var rarity)) return rarity;
        }
        return Rarity.Bronze;
    }

    private record ItemPageData(
        string? Name, string[]? Tags, string[]? HeroBadges,
        string? Description, EnchantmentData[]? Enchantments);

    private record EnchantmentData(string? Name, string? Tooltip);
}
```

- [ ] **Step 2: Create SkillScraper**

Create `src/BazaarOverlay.Infrastructure/Scraping/SkillScraper.cs`:

```csharp
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;

namespace BazaarOverlay.Infrastructure.Scraping;

public class SkillScraper : ScraperBase
{
    public SkillScraper(ILogger logger) : base(logger) { }

    public async Task<List<Skill>> ScrapeAllAsync(IPage page,
        IReadOnlyDictionary<string, Hero> heroes,
        IProgress<string>? progress = null)
    {
        var links = await CollectCardLinksAsync(page, "skills");
        var skills = new List<Skill>();

        for (var i = 0; i < links.Count; i++)
        {
            progress?.Report($"Scraping skills... ({i + 1}/{links.Count})");
            try
            {
                await NavigateToDetailPageAsync(page, links[i]);
                var skill = await ParseSkillPageAsync(page, links[i], heroes);
                if (skill is not null)
                    skills.Add(skill);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to scrape skill at {Link}", links[i]);
            }
        }

        Logger.LogInformation("Scraped {Count} skills", skills.Count);
        return skills;
    }

    private async Task<Skill?> ParseSkillPageAsync(IPage page, string href,
        IReadOnlyDictionary<string, Hero> heroes)
    {
        var parts = href.Split('/');
        var bazaarDbId = parts.Length >= 3 ? parts[2] : null;

        var data = await page.EvaluateFunctionAsync<SkillPageData>(@"() => {
            const name = document.querySelector('h1')?.textContent?.replace('Copy link', '')?.trim();
            const tagLinks = [...document.querySelectorAll('a[href*=""search?q=t%3A""]')];
            const tags = tagLinks.map(a => a.textContent.trim());
            const heroBadges = [...document.querySelectorAll('a[href*=""search?q=""]')]
                .map(a => a.textContent.trim())
                .filter(t => t.length === 3 && t === t.toUpperCase());
            return { name, tags, heroBadges };
        }");

        if (string.IsNullOrWhiteSpace(data?.Name))
            return null;

        var rarity = ParseRarityFromTags(data.Tags);
        var skill = new Skill(data.Name, rarity, bazaarDbId: bazaarDbId);

        var heroAbbrevMap = heroes.Values.ToDictionary(h => h.Abbreviation, h => h, StringComparer.OrdinalIgnoreCase);
        if (data.HeroBadges?.Any() == true)
        {
            foreach (var badge in data.HeroBadges)
            {
                if (heroAbbrevMap.TryGetValue(badge, out var hero))
                    skill.Heroes.Add(hero);
            }
        }
        else if (heroes.TryGetValue("Neutral", out var neutral))
        {
            skill.Heroes.Add(neutral);
        }

        var skipTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "Bronze", "Silver", "Gold", "Diamond", "Legendary",
              "Bronze+", "Silver+", "Gold+", "Diamond+", "Skill" };
        foreach (var tag in data.Tags ?? [])
        {
            if (!skipTags.Contains(tag) && tag.Length > 3)
                skill.Tags.Add(new SkillTag(tag));
        }

        return skill;
    }

    private static Rarity ParseRarityFromTags(IEnumerable<string>? tags)
    {
        if (tags is null) return Rarity.Bronze;
        foreach (var tag in tags)
        {
            var clean = tag.TrimEnd('+');
            if (Enum.TryParse<Rarity>(clean, true, out var rarity)) return rarity;
        }
        return Rarity.Bronze;
    }

    private record SkillPageData(string? Name, string[]? Tags, string[]? HeroBadges);
}
```

- [ ] **Step 3: Create MonsterScraper**

Create `src/BazaarOverlay.Infrastructure/Scraping/MonsterScraper.cs`:

```csharp
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;

namespace BazaarOverlay.Infrastructure.Scraping;

public class MonsterScraper : ScraperBase
{
    public MonsterScraper(ILogger logger) : base(logger) { }

    public async Task<List<Monster>> ScrapeAllAsync(IPage page, IProgress<string>? progress = null)
    {
        var links = await CollectCardLinksAsync(page, "monsters");
        var monsters = new List<Monster>();

        for (var i = 0; i < links.Count; i++)
        {
            progress?.Report($"Scraping monsters... ({i + 1}/{links.Count})");
            try
            {
                await NavigateToDetailPageAsync(page, links[i]);
                var monster = await ParseMonsterPageAsync(page, links[i]);
                if (monster is not null)
                    monsters.Add(monster);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to scrape monster at {Link}", links[i]);
            }
        }

        Logger.LogInformation("Scraped {Count} monsters", monsters.Count);
        return monsters;
    }

    private async Task<Monster?> ParseMonsterPageAsync(IPage page, string href)
    {
        var parts = href.Split('/');
        var bazaarDbId = parts.Length >= 3 ? parts[2] : null;

        var data = await page.EvaluateFunctionAsync<MonsterPageData>(@"() => {
            const name = document.querySelector('h1')?.textContent?.replace('Copy link', '')?.trim();
            const tagLinks = [...document.querySelectorAll('a[href*=""search?q=t%3A""]')];
            const tags = tagLinks.map(a => a.textContent.trim());

            // Extract Level, Health, Gold, XP from sidebar text
            const allText = document.body.innerText;
            const levelMatch = allText.match(/Level[:\s]*(\d+)/i);
            const healthMatch = allText.match(/Health[:\s]*(\d+)/i) ?? allText.match(/(\d+)\s*(?:HP|health)/i);
            const goldMatch = allText.match(/(\d+)\s*Gold/);
            const xpMatch = allText.match(/(\d+)\s*XP/);
            const dayMatch = allText.match(/Day\s*(\d+)/);

            return {
                name, tags,
                level: levelMatch ? parseInt(levelMatch[1]) : 1,
                health: healthMatch ? parseInt(healthMatch[1]) : 100,
                goldReward: goldMatch ? parseInt(goldMatch[1]) : 0,
                xpReward: xpMatch ? parseInt(xpMatch[1]) : 0,
                day: dayMatch ? parseInt(dayMatch[1]) : 1
            };
        }");

        if (string.IsNullOrWhiteSpace(data?.Name))
            return null;

        var tier = ParseRarityFromTags(data.Tags);
        return new Monster(data.Name, tier, data.Level, data.Day,
            data.Health, data.GoldReward, data.XpReward, bazaarDbId);
    }

    private static Rarity ParseRarityFromTags(IEnumerable<string>? tags)
    {
        if (tags is null) return Rarity.Bronze;
        foreach (var tag in tags)
        {
            if (Enum.TryParse<Rarity>(tag, true, out var rarity)) return rarity;
        }
        return Rarity.Bronze;
    }

    private record MonsterPageData(
        string? Name, string[]? Tags, int Level, int Health,
        int GoldReward, int XpReward, int Day);
}
```

- [ ] **Step 4: Create MerchantScraper**

Create `src/BazaarOverlay.Infrastructure/Scraping/MerchantScraper.cs`:

```csharp
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;

namespace BazaarOverlay.Infrastructure.Scraping;

public class MerchantScraper : ScraperBase
{
    public MerchantScraper(ILogger logger) : base(logger) { }

    public async Task<List<Merchant>> ScrapeAllAsync(IPage page, IProgress<string>? progress = null)
    {
        var links = await CollectCardLinksAsync(page, "merchants");
        var merchants = new List<Merchant>();

        for (var i = 0; i < links.Count; i++)
        {
            progress?.Report($"Scraping merchants... ({i + 1}/{links.Count})");
            try
            {
                await NavigateToDetailPageAsync(page, links[i]);
                var merchant = await ParseMerchantPageAsync(page, links[i]);
                if (merchant is not null)
                    merchants.Add(merchant);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to scrape merchant at {Link}", links[i]);
            }
        }

        Logger.LogInformation("Scraped {Count} merchants", merchants.Count);
        return merchants;
    }

    private async Task<Merchant?> ParseMerchantPageAsync(IPage page, string href)
    {
        var parts = href.Split('/');
        var bazaarDbId = parts.Length >= 3 ? parts[2] : null;

        var data = await page.EvaluateFunctionAsync<MerchantPageData>(@"() => {
            const name = document.querySelector('h1')?.textContent?.replace('Copy link', '')?.trim();
            const tagLinks = [...document.querySelectorAll('a[href*=""search?q=t%3A""]')];
            const tags = tagLinks.map(a => a.textContent.trim());

            // Tooltip from sidebar
            const allText = document.body.innerText;

            // Selection rules from 'Selection Rules' section
            const selectMatch = allText.match(/Select[:\s]*(You (?:are able to select|can only select)[^.]*\.)/i);
            const costMatch = allText.match(/Cost[:\s]*(You must pay[^.]*\.)/i);
            const leaveMatch = allText.match(/Leave[:\s]*(You can leave[^.]*\.)/i);
            const rollsMatch = allText.match(/(\d+)\s*time\(s\)/);
            const rerollCostMatch = allText.match(/Rerolling costs?\s*(\d+)\s*gold/i);

            return {
                name, tags,
                selectRule: selectMatch?.[1] ?? null,
                costRule: costMatch?.[1] ?? null,
                leaveRule: leaveMatch?.[1] ?? null,
                rerollCount: rollsMatch ? parseInt(rollsMatch[1]) : null,
                rerollCost: rerollCostMatch ? parseInt(rerollCostMatch[1]) : null
            };
        }");

        if (string.IsNullOrWhiteSpace(data?.Name))
            return null;

        var tier = ParseRarityFromTags(data.Tags);

        return new Merchant(data.Name, tier,
            selectionRule: data.SelectRule,
            costRule: data.CostRule,
            leaveRule: data.LeaveRule,
            rerollCount: data.RerollCount,
            rerollCost: data.RerollCost,
            bazaarDbId: bazaarDbId);
    }

    private static Rarity ParseRarityFromTags(IEnumerable<string>? tags)
    {
        if (tags is null) return Rarity.Bronze;
        foreach (var tag in tags)
        {
            if (Enum.TryParse<Rarity>(tag, true, out var rarity)) return rarity;
        }
        return Rarity.Bronze;
    }

    private record MerchantPageData(
        string? Name, string[]? Tags, string? SelectRule, string? CostRule,
        string? LeaveRule, int? RerollCount, int? RerollCost);
}
```

- [ ] **Step 5: Create TrainerScraper**

Create `src/BazaarOverlay.Infrastructure/Scraping/TrainerScraper.cs` — same pattern as MerchantScraper but uses `trainers` category. Nearly identical structure; swap `Merchant` → `Trainer` and `ItemPool` → `SkillPool`.

```csharp
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;

namespace BazaarOverlay.Infrastructure.Scraping;

public class TrainerScraper : ScraperBase
{
    public TrainerScraper(ILogger logger) : base(logger) { }

    public async Task<List<Trainer>> ScrapeAllAsync(IPage page, IProgress<string>? progress = null)
    {
        var links = await CollectCardLinksAsync(page, "trainers");
        var trainers = new List<Trainer>();

        for (var i = 0; i < links.Count; i++)
        {
            progress?.Report($"Scraping trainers... ({i + 1}/{links.Count})");
            try
            {
                await NavigateToDetailPageAsync(page, links[i]);
                var trainer = await ParseTrainerPageAsync(page, links[i]);
                if (trainer is not null)
                    trainers.Add(trainer);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to scrape trainer at {Link}", links[i]);
            }
        }

        Logger.LogInformation("Scraped {Count} trainers", trainers.Count);
        return trainers;
    }

    private async Task<Trainer?> ParseTrainerPageAsync(IPage page, string href)
    {
        var parts = href.Split('/');
        var bazaarDbId = parts.Length >= 3 ? parts[2] : null;

        var data = await page.EvaluateFunctionAsync<TrainerPageData>(@"() => {
            const name = document.querySelector('h1')?.textContent?.replace('Copy link', '')?.trim();
            const tagLinks = [...document.querySelectorAll('a[href*=""search?q=t%3A""]')];
            const tags = tagLinks.map(a => a.textContent.trim());
            const allText = document.body.innerText;

            const selectMatch = allText.match(/Select[:\s]*(You can only select[^.]*\.)/i);
            const costMatch = allText.match(/Cost[:\s]*(Skills you select[^.]*\.)/i);
            const leaveMatch = allText.match(/Leave[:\s]*(You can leave[^.]*\.)/i);
            const rollsMatch = allText.match(/(\d+)\s*time\(s\)/);
            const rerollCostMatch = allText.match(/Rerolling costs?\s*(\d+)\s*gold/i);

            return {
                name, tags,
                selectRule: selectMatch?.[1] ?? null,
                costRule: costMatch?.[1] ?? null,
                leaveRule: leaveMatch?.[1] ?? null,
                rerollCount: rollsMatch ? parseInt(rollsMatch[1]) : null,
                rerollCost: rerollCostMatch ? parseInt(rerollCostMatch[1]) : null
            };
        }");

        if (string.IsNullOrWhiteSpace(data?.Name))
            return null;

        var tier = ParseRarityFromTags(data.Tags);

        return new Trainer(data.Name, tier,
            selectionRule: data.SelectRule,
            costRule: data.CostRule,
            leaveRule: data.LeaveRule,
            rerollCount: data.RerollCount,
            rerollCost: data.RerollCost,
            bazaarDbId: bazaarDbId);
    }

    private static Rarity ParseRarityFromTags(IEnumerable<string>? tags)
    {
        if (tags is null) return Rarity.Bronze;
        foreach (var tag in tags)
        {
            if (Enum.TryParse<Rarity>(tag, true, out var rarity)) return rarity;
        }
        return Rarity.Bronze;
    }

    private record TrainerPageData(
        string? Name, string[]? Tags, string? SelectRule, string? CostRule,
        string? LeaveRule, int? RerollCount, int? RerollCost);
}
```

- [ ] **Step 6: Create EventScraper**

Create `src/BazaarOverlay.Infrastructure/Scraping/EventScraper.cs`:

```csharp
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;

namespace BazaarOverlay.Infrastructure.Scraping;

public class EventScraper : ScraperBase
{
    public EventScraper(ILogger logger) : base(logger) { }

    public async Task<List<Event>> ScrapeAllAsync(IPage page, IProgress<string>? progress = null)
    {
        var links = await CollectCardLinksAsync(page, "events");
        var events = new List<Event>();

        for (var i = 0; i < links.Count; i++)
        {
            progress?.Report($"Scraping events... ({i + 1}/{links.Count})");
            try
            {
                await NavigateToDetailPageAsync(page, links[i]);
                var evt = await ParseEventPageAsync(page, links[i]);
                if (evt is not null)
                    events.Add(evt);
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to scrape event at {Link}", links[i]);
            }
        }

        Logger.LogInformation("Scraped {Count} events", events.Count);
        return events;
    }

    private async Task<Event?> ParseEventPageAsync(IPage page, string href)
    {
        var parts = href.Split('/');
        var bazaarDbId = parts.Length >= 3 ? parts[2] : null;

        var data = await page.EvaluateFunctionAsync<EventPageData>(@"() => {
            const name = document.querySelector('h1')?.textContent?.replace('Copy link', '')?.trim();
            const tagLinks = [...document.querySelectorAll('a[href*=""search?q=t%3A""]')];
            const tags = tagLinks.map(a => a.textContent.trim());
            const allText = document.body.innerText;

            const selectMatch = allText.match(/Select[:\s]*(You can only select[^.]*\.)/i);
            const costMatch = allText.match(/Cost[:\s]*(You must pay[^.]*\.)/i);
            const leaveMatch = allText.match(/Leave[:\s]*(You can leave[^.]*\.)/i);

            // Parse options from H3 headings under the Options section
            const h3s = [...document.querySelectorAll('h3')];
            const options = h3s
                .filter(h => h.closest('section, div')?.querySelector('h2')?.textContent?.includes('Options'))
                .map(h => ({
                    name: h.textContent.trim(),
                    description: h.nextElementSibling?.textContent?.trim() ?? null
                }));

            return {
                name, tags,
                selectRule: selectMatch?.[1] ?? null,
                costRule: costMatch?.[1] ?? null,
                leaveRule: leaveMatch?.[1] ?? null,
                options
            };
        }");

        if (string.IsNullOrWhiteSpace(data?.Name))
            return null;

        var tier = ParseRarityFromTags(data.Tags);

        var evt = new Event(data.Name, tier,
            selectionRule: data.SelectRule,
            costRule: data.CostRule,
            leaveRule: data.LeaveRule,
            bazaarDbId: bazaarDbId);

        foreach (var opt in data.Options ?? [])
        {
            if (!string.IsNullOrWhiteSpace(opt.Name))
                evt.Options.Add(new EventOption(opt.Name, tier, opt.Description));
        }

        return evt;
    }

    private static Rarity ParseRarityFromTags(IEnumerable<string>? tags)
    {
        if (tags is null) return Rarity.Bronze;
        foreach (var tag in tags)
        {
            if (Enum.TryParse<Rarity>(tag, true, out var rarity)) return rarity;
        }
        return Rarity.Bronze;
    }

    private record EventPageData(
        string? Name, string[]? Tags, string? SelectRule, string? CostRule,
        string? LeaveRule, OptionData[]? Options);

    private record OptionData(string? Name, string? Description);
}
```

- [ ] **Step 7: Verify build**

```bash
dotnet build src/BazaarOverlay.Infrastructure --no-restore
```

Expected: SUCCESS

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "Create category scrapers for items, skills, monsters, merchants, trainers, events"
```

**Note:** The JS selectors in these scrapers are best-effort based on site investigation. They will likely need refinement during the first live scrape run. This is expected — adjust selectors based on actual DOM structure when running against the site.

---

## Task 14: Create BazaarDbScraper Orchestrator

**Files:**
- Create: `src/BazaarOverlay.Infrastructure/Scraping/BazaarDbScraper.cs`

- [ ] **Step 1: Implement BazaarDbScraper**

Create `src/BazaarOverlay.Infrastructure/Scraping/BazaarDbScraper.cs`:

```csharp
using BazaarOverlay.Domain.Entities;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;

namespace BazaarOverlay.Infrastructure.Scraping;

public class BazaarDbScraper : IBazaarDbScraper
{
    private readonly ILogger<BazaarDbScraper> _logger;
    private IBrowser? _browser;
    private IPage? _page;

    public BazaarDbScraper(ILogger<BazaarDbScraper> logger)
    {
        _logger = logger;
    }

    public async Task InitializeAsync(IProgress<string>? progress = null)
    {
        progress?.Report("Downloading browser (first time only)...");
        var browserFetcher = new BrowserFetcher();
        await browserFetcher.DownloadAsync();

        progress?.Report("Launching browser...");
        _browser = await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
        _page = await _browser.NewPageAsync();
        await _page.SetViewportAsync(new ViewPortOptions { Width = 1280, Height = 900 });
        _logger.LogInformation("Browser initialized");
    }

    public async Task<IReadOnlyList<Item>> ScrapeItemsAsync(
        IReadOnlyDictionary<string, Hero> heroes,
        IReadOnlyDictionary<string, Enchantment> enchantments,
        IProgress<string>? progress = null)
    {
        EnsureInitialized();
        var scraper = new ItemScraper(_logger);
        return await scraper.ScrapeAllAsync(_page!, heroes, enchantments, progress);
    }

    public async Task<IReadOnlyList<Skill>> ScrapeSkillsAsync(
        IReadOnlyDictionary<string, Hero> heroes,
        IProgress<string>? progress = null)
    {
        EnsureInitialized();
        var scraper = new SkillScraper(_logger);
        return await scraper.ScrapeAllAsync(_page!, heroes, progress);
    }

    public async Task<IReadOnlyList<Monster>> ScrapeMonstersAsync(IProgress<string>? progress = null)
    {
        EnsureInitialized();
        var scraper = new MonsterScraper(_logger);
        return await scraper.ScrapeAllAsync(_page!, progress);
    }

    public async Task<IReadOnlyList<Merchant>> ScrapeMerchantsAsync(IProgress<string>? progress = null)
    {
        EnsureInitialized();
        var scraper = new MerchantScraper(_logger);
        return await scraper.ScrapeAllAsync(_page!, progress);
    }

    public async Task<IReadOnlyList<Trainer>> ScrapeTrainersAsync(IProgress<string>? progress = null)
    {
        EnsureInitialized();
        var scraper = new TrainerScraper(_logger);
        return await scraper.ScrapeAllAsync(_page!, progress);
    }

    public async Task<IReadOnlyList<Event>> ScrapeEventsAsync(IProgress<string>? progress = null)
    {
        EnsureInitialized();
        var scraper = new EventScraper(_logger);
        return await scraper.ScrapeAllAsync(_page!, progress);
    }

    private void EnsureInitialized()
    {
        if (_browser is null || _page is null)
            throw new InvalidOperationException("Call InitializeAsync before scraping.");
    }

    public void Dispose()
    {
        _page?.Dispose();
        _browser?.Dispose();
    }
}
```

- [ ] **Step 2: Verify build**

```bash
dotnet build src/BazaarOverlay.Infrastructure --no-restore
```

Expected: SUCCESS

- [ ] **Step 3: Commit**

```bash
git add src/BazaarOverlay.Infrastructure/Scraping/BazaarDbScraper.cs
git commit -m "Create BazaarDbScraper orchestrator with browser lifecycle management"
```

---

## Task 15: Rewrite DataImportService

**Files:**
- Modify: `src/BazaarOverlay.Infrastructure/DataImport/DataImportService.cs`
- Test: `tests/BazaarOverlay.Tests/Infrastructure/DataImportTests.cs`

- [ ] **Step 1: Rewrite DataImportService**

Replace `src/BazaarOverlay.Infrastructure/DataImport/DataImportService.cs`:

```csharp
using BazaarOverlay.Application.Interfaces;
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Enums;
using BazaarOverlay.Infrastructure.Persistence;
using BazaarOverlay.Infrastructure.Scraping;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BazaarOverlay.Infrastructure.DataImport;

public class DataImportService : IDataImportService
{
    private readonly BazaarDbContext _context;
    private readonly IBazaarDbScraper _scraper;
    private readonly ILogger<DataImportService> _logger;
    private readonly string _dbPath;

    public DataImportService(BazaarDbContext context, IBazaarDbScraper scraper,
        ILogger<DataImportService> logger, string dbPath)
    {
        _context = context;
        _scraper = scraper;
        _logger = logger;
        _dbPath = dbPath;
    }

    public async Task ImportAllAsync(IProgress<string>? progress = null)
    {
        _logger.LogInformation("Starting full data import from bazaardb.gg...");

        // Delete and recreate database
        progress?.Report("Resetting database...");
        _context.Database.EnsureDeleted();
        _context.Database.EnsureCreated();

        // Seed static data
        progress?.Report("Seeding heroes...");
        var heroes = await SeedHeroesAsync();

        progress?.Report("Seeding enchantments...");
        var enchantments = await SeedEnchantmentsAsync();

        // Initialize browser
        await _scraper.InitializeAsync(progress);

        // Scrape and save each category
        progress?.Report("Scraping items...");
        var items = await _scraper.ScrapeItemsAsync(heroes, enchantments, progress);
        _context.Items.AddRange(items);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Saved {Count} items", items.Count);

        progress?.Report("Scraping skills...");
        var skills = await _scraper.ScrapeSkillsAsync(heroes, progress);
        _context.Skills.AddRange(skills);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Saved {Count} skills", skills.Count);

        progress?.Report("Scraping monsters...");
        var monsters = await _scraper.ScrapeMonstersAsync(progress);
        _context.Monsters.AddRange(monsters);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Saved {Count} monsters", monsters.Count);

        progress?.Report("Scraping merchants...");
        var merchants = await _scraper.ScrapeMerchantsAsync(progress);
        _context.Merchants.AddRange(merchants);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Saved {Count} merchants", merchants.Count);

        progress?.Report("Scraping trainers...");
        var trainers = await _scraper.ScrapeTrainersAsync(progress);
        _context.Trainers.AddRange(trainers);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Saved {Count} trainers", trainers.Count);

        progress?.Report("Scraping events...");
        var events = await _scraper.ScrapeEventsAsync(progress);
        _context.Events.AddRange(events);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Saved {Count} events", events.Count);

        _logger.LogInformation("Full data import completed");
        progress?.Report("Import complete!");
    }

    private async Task<IReadOnlyDictionary<string, Hero>> SeedHeroesAsync()
    {
        var heroData = new (string Name, string Abbrev)[]
        {
            ("Dooley", "DOO"), ("Jules", "JUL"), ("Karnok", "KAR"),
            ("Mak", "MAK"), ("Pygmalien", "PYG"), ("Stelle", "STE"),
            ("Vanessa", "VAN"), ("Neutral", "NEU")
        };

        foreach (var (name, abbrev) in heroData)
            _context.Heroes.Add(new Hero(name, abbrev));

        await _context.SaveChangesAsync();

        return await _context.Heroes.ToDictionaryAsync(h => h.Name, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<IReadOnlyDictionary<string, Enchantment>> SeedEnchantmentsAsync()
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
            _context.Enchantments.Add(new Enchantment(name, desc));

        await _context.SaveChangesAsync();

        return await _context.Enchantments.ToDictionaryAsync(e => e.Name, StringComparer.OrdinalIgnoreCase);
    }
}
```

- [ ] **Step 2: Update DataImportTests**

Update `tests/BazaarOverlay.Tests/Infrastructure/DataImportTests.cs` to work with the new `DataImportService` constructor signature. The test should verify hero and enchantment seeding at minimum, since scraping requires a live browser. Mock `IBazaarDbScraper` to return empty lists for the scraping methods.

- [ ] **Step 3: Run tests**

```bash
dotnet test tests/BazaarOverlay.Tests --no-restore
```

Expected: PASS

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "Rewrite DataImportService to use bazaardb.gg scraper"
```

---

## Task 16: Update WPF Layer

**Files:**
- Modify: `src/BazaarOverlay.WPF/App.xaml.cs`
- Modify: `src/BazaarOverlay.WPF/ViewModels/MainViewModel.cs`
- Modify: `src/BazaarOverlay.WPF/ViewModels/MonsterEncounterViewModel.cs`

- [ ] **Step 1: Update App.xaml.cs DI registrations**

Replace `src/BazaarOverlay.WPF/App.xaml.cs`:

```csharp
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
using BazaarOverlay.Infrastructure.Scraping;
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
            options.UseSqlite($"Data Source={dbPath}",
                o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

        // Repositories
        services.AddScoped<IHeroRepository, HeroRepository>();
        services.AddScoped<IMonsterRepository, MonsterRepository>();
        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<ISkillRepository, SkillRepository>();
        services.AddScoped<IMerchantRepository, MerchantRepository>();
        services.AddScoped<ITrainerRepository, TrainerRepository>();
        services.AddScoped<IEventRepository, EventRepository>();

        // Application services
        services.AddSingleton<IGameSessionService, GameSessionService>();
        services.AddScoped<IMonsterService, MonsterService>();
        services.AddScoped<IItemInfoService, ItemInfoService>();
        services.AddScoped<ISkillInfoService, SkillInfoService>();
        services.AddScoped<IMerchantService, MerchantService>();
        services.AddScoped<ITrainerService, TrainerService>();
        services.AddScoped<IEventService, EventService>();

        // Scraping and import
        services.AddScoped<IBazaarDbScraper, BazaarDbScraper>();
        services.AddScoped<IDataImportService>(sp =>
            new DataImportService(
                sp.GetRequiredService<BazaarDbContext>(),
                sp.GetRequiredService<IBazaarDbScraper>(),
                sp.GetRequiredService<ILogger<DataImportService>>(),
                dbPath));

        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
            builder.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);
        });

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
```

- [ ] **Step 2: Update MainViewModel**

Replace `src/BazaarOverlay.WPF/ViewModels/MainViewModel.cs`:

```csharp
using System.Collections.ObjectModel;
using BazaarOverlay.Application.Interfaces;
using BazaarOverlay.Domain.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace BazaarOverlay.WPF.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IGameSessionService _gameSession;
    private readonly IHeroRepository _heroRepository;
    private readonly IDataImportService _dataImportService;
    private readonly ILogger<MainViewModel> _logger;

    [ObservableProperty]
    private string? _selectedHero;

    [ObservableProperty]
    private int _currentDay = 1;

    [ObservableProperty]
    private bool _isHeroSelected;

    [ObservableProperty]
    private bool _isImporting;

    [ObservableProperty]
    private string _importStatus = string.Empty;

    public ObservableCollection<string> Heroes { get; } = new();

    public MonsterEncounterViewModel MonsterEncounter { get; }
    public ItemSkillInfoViewModel ItemSkillInfo { get; }

    public MainViewModel(
        IGameSessionService gameSession,
        IHeroRepository heroRepository,
        IDataImportService dataImportService,
        MonsterEncounterViewModel monsterEncounterViewModel,
        ItemSkillInfoViewModel itemSkillInfoViewModel,
        ILogger<MainViewModel> logger)
    {
        _gameSession = gameSession;
        _heroRepository = heroRepository;
        _dataImportService = dataImportService;
        MonsterEncounter = monsterEncounterViewModel;
        ItemSkillInfo = itemSkillInfoViewModel;
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        var heroes = await _heroRepository.GetAllAsync();
        Heroes.Clear();
        foreach (var hero in heroes.Where(h => h.Name != "Neutral"))
        {
            Heroes.Add(hero.Name);
        }
    }

    partial void OnSelectedHeroChanged(string? value)
    {
        if (value is not null)
        {
            _gameSession.SelectHero(value);
            CurrentDay = _gameSession.CurrentDay;
            IsHeroSelected = true;
        }
    }

    [RelayCommand]
    private void AdvanceDay()
    {
        _gameSession.AdvanceDay();
        CurrentDay = _gameSession.CurrentDay;
    }

    [RelayCommand]
    private async Task DownloadDataAsync()
    {
        if (IsImporting)
            return;

        IsImporting = true;
        try
        {
            var progress = new Progress<string>(msg => ImportStatus = msg);
            await _dataImportService.ImportAllAsync(progress);
            await InitializeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Download data failed");
            ImportStatus = $"Error: {ex.Message}";
        }
        finally
        {
            IsImporting = false;
        }
    }
}
```

- [ ] **Step 3: Update MonsterEncounterViewModel**

Replace `src/BazaarOverlay.WPF/ViewModels/MonsterEncounterViewModel.cs`:

```csharp
using System.Collections.ObjectModel;
using BazaarOverlay.Application.DTOs;
using BazaarOverlay.Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BazaarOverlay.WPF.ViewModels;

public partial class MonsterEncounterViewModel : ObservableObject
{
    private readonly IMonsterService _monsterService;
    private readonly IGameSessionService _gameSession;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private MonsterResult? _selectedMonster;

    [ObservableProperty]
    private bool _isSearching;

    public ObservableCollection<MonsterResult> SearchResults { get; } = new();

    public MonsterEncounterViewModel(
        IMonsterService monsterService,
        IGameSessionService gameSession)
    {
        _monsterService = monsterService;
        _gameSession = gameSession;
    }

    partial void OnSearchTextChanged(string value)
    {
        if (value.Length >= 2)
        {
            _ = SearchAsync(value);
        }
        else
        {
            SearchResults.Clear();
            SelectedMonster = null;
        }
    }

    [RelayCommand]
    private async Task SearchAsync(string? searchText = null)
    {
        var query = searchText ?? SearchText;
        if (string.IsNullOrWhiteSpace(query))
            return;

        IsSearching = true;
        try
        {
            var results = await _monsterService.SearchMonstersAsync(query);

            SearchResults.Clear();
            foreach (var result in results)
            {
                SearchResults.Add(result);
            }

            SelectedMonster = SearchResults.FirstOrDefault();
        }
        finally
        {
            IsSearching = false;
        }
    }
}
```

- [ ] **Step 4: Verify full build**

```bash
dotnet build BazaarOverlay.sln --no-restore
```

Expected: SUCCESS (or identify any remaining compilation issues)

- [ ] **Step 5: Run all tests**

```bash
dotnet test tests/BazaarOverlay.Tests --no-restore
```

Expected: PASS

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "Update WPF layer for bazaardb.gg scraping migration"
```

---

## Task 17: Clean Up and Final Verification

- [ ] **Step 1: Remove MockHttpMessageHandler if no longer used**

Check if `tests/BazaarOverlay.Tests/Helpers/MockHttpMessageHandler.cs` is still referenced. If not, delete it.

- [ ] **Step 2: Remove HttpClient registration from App.xaml.cs**

The `services.AddHttpClient()` line was removed in Task 16 since we no longer use `BazaarPlannerImporter`. Verify it's gone.

- [ ] **Step 3: Run full test suite**

```bash
dotnet test tests/BazaarOverlay.Tests --no-restore -v normal
```

Expected: ALL PASS

- [ ] **Step 4: Run full build**

```bash
dotnet build BazaarOverlay.sln
```

Expected: SUCCESS, 0 warnings (or only expected ones)

- [ ] **Step 5: Commit and push**

```bash
git add -A
git commit -m "Final cleanup for bazaardb.gg scraping migration"
git push origin main
```

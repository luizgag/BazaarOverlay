# BazaarDB.gg Web Scraping Migration

## Summary

Migrate the data import pipeline from BazaarPlanner GitHub (JS file parsing) to web scraping bazaardb.gg using PuppeteerSharp. Refactor the domain model to use distinct entities for Monsters, Merchants, Trainers, and Events instead of the current Encounter umbrella. Capture practical overlay data including item enchantment descriptions.

## Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Data source | bazaardb.gg | Richer data than BazaarPlanner GitHub, includes enchantments, merchants, trainers, events |
| Scraping approach | Page-per-entity via Playwright | Gets complete data per entity; consistent parsing strategy |
| Browser library | PuppeteerSharp | Auto-downloads Chromium, clean async C# API, no manual install |
| Data depth | Practical overlay data | Name, size, tier, cooldown, tags, heroes, tier values, enchantment descriptions. Skip deep mechanics and patch history |
| Entity modeling | Separate entities | Merchant, Trainer, Event, Monster as distinct domain entities with tailored fields |
| Import strategy | Full wipe-and-reimport | Delete database file, recreate schema, scrape everything fresh |

## Data Source: bazaardb.gg

- **URL pattern**: List pages at `/search?c={category}`, detail pages at `/card/{id}/{slug}`
- **Protection**: Cloudflare Turnstile (invisible challenge), requires real browser
- **No public API**: `/api/search` returns 401
- **Entity counts**: Items 1,036 | Skills 415 | Merchants 47 | Trainers 23 | Monsters 127 | Events 48

## Domain Model

### Hero (modified)

| Field | Type | Notes |
|-------|------|-------|
| Name | string (PK, max 50) | Unchanged |
| Abbreviation | string (max 3) | New. "KAR", "DOO", etc. |

Seeded: Dooley (DOO), Jules (JUL), Mak (MAK), Pygmalien (PYG), Stelle (STE), Vanessa (VAN), Karnok (KAR), Neutral (NEU).

### Item (modified)

| Field | Type | Notes |
|-------|------|-------|
| Id | int (PK, auto) | Unchanged |
| Name | string (max 100, unique) | Unchanged |
| Size | ItemSize enum | Unchanged |
| MinimumRarity | Rarity enum | Unchanged |
| Cooldown | decimal? | Unchanged |
| Cost | string? (max 100) | New. Tier-scaled, e.g. "8 >> 16 >> 32" |
| Value | string? (max 100) | New. Sell price, e.g. "4 >> 8 >> 16" |
| Description | string? (max 500) | New. Tier-scaled tooltip text |
| BazaarDbId | string (max 50) | New. bazaardb.gg card ID for future reference |

Collections: Heroes (M2M), Tags (1:M), TierValues (1:M), Enchantments (1:M via ItemEnchantment).

### Skill (modified)

| Field | Type | Notes |
|-------|------|-------|
| Id | int (PK, auto) | Unchanged |
| Name | string (max 100, unique) | Unchanged |
| MinimumRarity | Rarity enum | Unchanged |
| Cost | string? (max 100) | New. Tier-scaled |
| Description | string? (max 500) | New. Tier-scaled tooltip |
| BazaarDbId | string (max 50) | New |

Collections: Heroes (M2M), Tags (1:M), TierValues (1:M).

### Enchantment (unchanged)

| Field | Type |
|-------|------|
| Id | int (PK) |
| Name | string (max 50, unique) |
| GlobalDescription | string (max 200) |

Seeded: Deadly, Fiery, Golden, Heavy, Icy, Obsidian, Radiant, Restorative, Shielded, Shiny, Toxic, Turbo.

### ItemEnchantment (unchanged)

| Field | Type |
|-------|------|
| Id | int (PK, auto) |
| ItemId | int (FK) |
| EnchantmentId | int (FK) |
| EffectDescription | string (max 500) |

### Monster (new, replaces old Monster)

| Field | Type | Notes |
|-------|------|-------|
| Id | int (PK, auto) | |
| Name | string (max 100, unique) | |
| Tier | Rarity enum | Bronze/Silver/Gold/Diamond/Legendary |
| Level | int | Monster level |
| Day | int | Day monster first appears |
| Health | int | |
| GoldReward | int | Gold earned on defeat |
| XpReward | int | XP earned on defeat |
| BazaarDbId | string (max 50) | |

Collections: BoardItems (M2M with Item), BoardSkills (M2M with Skill).

### Merchant (new)

| Field | Type | Notes |
|-------|------|-------|
| Id | int (PK, auto) | |
| Name | string (max 100, unique) | |
| Tier | Rarity enum | |
| Tooltip | string (max 500) | What the merchant sells |
| SelectionRule | string? (max 200) | "select multiple" / "select one" |
| CostRule | string? (max 200) | Cost description |
| LeaveRule | string? (max 200) | Leave description |
| RerollCount | int? | Number of rerolls allowed |
| RerollCost | int? | Gold cost per reroll |
| BazaarDbId | string (max 50) | |

Collections: ItemPool (M2M with Item).

### Trainer (new)

| Field | Type | Notes |
|-------|------|-------|
| Id | int (PK, auto) | |
| Name | string (max 100, unique) | |
| Tier | Rarity enum | |
| Tooltip | string (max 500) | What the trainer teaches |
| SelectionRule | string? (max 200) | |
| CostRule | string? (max 200) | |
| LeaveRule | string? (max 200) | |
| RerollCount | int? | |
| RerollCost | int? | |
| BazaarDbId | string (max 50) | |

Collections: SkillPool (M2M with Skill).

### Event (new)

| Field | Type | Notes |
|-------|------|-------|
| Id | int (PK, auto) | |
| Name | string (max 100, unique) | |
| Tier | Rarity enum | |
| Tooltip | string? (max 500) | Flavor text |
| SelectionRule | string? (max 200) | |
| CostRule | string? (max 200) | |
| LeaveRule | string? (max 200) | |
| BazaarDbId | string (max 50) | |

Collections: Options (1:M with EventOption).

### EventOption (new)

| Field | Type | Notes |
|-------|------|-------|
| Id | int (PK, auto) | |
| EventId | int (FK) | |
| Name | string (max 200) | Option name |
| Tier | Rarity enum | |
| Description | string (max 500) | Effect description |
| HeroRestriction | string? (max 50) | Hero name if restricted |

### Removed Entities

- Encounter, ShopConstraints, ShopAllowedTag
- EncounterItemReward, EncounterSkillReward (join tables)
- MonsterItemDrop, MonsterSkillDrop (join tables)
- RarityDayProbability

### Enums

- **Rarity**: Bronze, Silver, Gold, Diamond, Legendary (unchanged)
- **ItemSize**: Small=1, Medium=2, Large=3 (unchanged)
- **EncounterType**: Removed

## Scraping Architecture

### Project Structure

```
Infrastructure/
  Scraping/
    IBazaarDbScraper.cs          -- interface for Application layer
    BazaarDbScraper.cs           -- orchestrator, browser lifecycle
    ScraperBase.cs               -- shared: navigate, wait, collect links, delays
    ItemScraper.cs               -- parse item detail pages + enchantments
    SkillScraper.cs              -- parse skill detail pages
    MonsterScraper.cs            -- parse monster detail pages + board
    MerchantScraper.cs           -- parse merchant detail pages + item pool
    TrainerScraper.cs            -- parse trainer detail pages + skill pool
    EventScraper.cs              -- parse event detail pages + options
```

### Browser Management

- PuppeteerSharp NuGet package in Infrastructure project
- `BrowserFetcher` auto-downloads Chromium on first use
- Single `IBrowser` instance per import session, headless mode
- Disposable pattern for cleanup
- Configurable page timeout (30s default)

### Scraping Flow

```
1. Delete database file
2. Recreate schema (EnsureCreated)
3. Seed heroes (7 + Neutral, with abbreviations)
4. Seed enchantments (12 hardcoded)
5. Launch headless browser
6. For each category in order [Items, Skills, Monsters, Merchants, Trainers, Events]:
   a. Navigate to /search?c={category}
   b. Click "Load more" repeatedly until all cards visible
   c. Collect all /card/{id}/{slug} hrefs
   d. For each href:
      - Navigate to detail page
      - Wait for content to render
      - Extract data via JS evaluation (querySelectorAll)
      - Map to domain entity
      - 500ms delay between pages
   e. Bulk save all entities for that category
7. Close browser
```

### Category Order Rationale

Items and Skills first because Monsters, Merchants, and Trainers reference them. Events are self-contained.

### Progress Reporting

Reuse existing `IProgress<string>` mechanism:
- "Downloading browser..." (first time only)
- "Scraping items... (42/1036)"
- "Saving items..."
- "Scraping skills... (100/415)"
- etc.

### Error Handling

- Individual page failures logged and skipped (don't abort entire import)
- Browser launch failure reported to user with actionable message
- Cloudflare challenge failures: retry once, then skip with warning

## What Gets Removed

### Infrastructure
- `BazaarPlannerImporter` and all BazaarPlanner DTOs
- `EncounterRepository`
- `RarityDayProbabilityRepository`
- Embedded rarity probability JSON seed data

### Domain
- `Encounter`, `ShopConstraints`, `ShopAllowedTag` entities
- `EncounterType` enum
- `RarityDayProbability` entity
- `IEncounterRepository`, `IRarityDayProbabilityRepository` interfaces

### Application
- `EncounterService`, `ShopService`, `MonsterEncounterService`
- All related interfaces and DTOs (`EncounterResult`, `ShopResult`, `MonsterEncounterResult`)

## What Gets Added

### Infrastructure
- `Scraping/` directory with 8 files (interface + orchestrator + base + 6 scrapers)
- `MonsterRepository`, `MerchantRepository`, `TrainerRepository`, `EventRepository`

### Domain
- `Monster`, `Merchant`, `Trainer`, `Event`, `EventOption` entities
- `IMonsterRepository`, `IMerchantRepository`, `ITrainerRepository`, `IEventRepository` interfaces
- `Abbreviation` field on Hero

### Application
- `IMonsterService`, `IMerchantService`, `ITrainerService`, `IEventService`
- Updated DTOs for items/skills (add Cost, Value, Description fields)

## What Gets Modified

### Infrastructure
- `BazaarDbContext` — remove old entity configs, add new ones
- `DataImportService` — rewire to use `IBazaarDbScraper`
- `ItemRepository` — update FullQuery if entity changes need it
- `MonsterRepository` — rewritten for new entity shape

### Application
- `ItemInfoService` — update DTOs with new fields
- `SkillInfoService` — update DTOs with new fields
- `GameSessionService` — unchanged (hero + day tracking still useful)

### WPF
- `App.xaml.cs` — update DI registrations
- `MainViewModel` — DownloadDataAsync triggers new scraping flow, remove rarity probability seeding from startup

### NuGet
- Add `PuppeteerSharp` to Infrastructure project

## Testing Strategy

- Unit tests for entity construction and validation
- Unit tests for DOM parsing logic (feed HTML fragments to scraper methods)
- Integration tests for repositories with in-memory SQLite
- Manual end-to-end test of full scrape flow

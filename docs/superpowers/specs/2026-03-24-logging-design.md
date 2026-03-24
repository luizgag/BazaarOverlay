# Logging Design

## Goal

Add development/debugging logging using `Microsoft.Extensions.Logging` with a console provider so developers can see errors and key operations in the console while building and testing.

## Approach

Direct `ILogger<T>` injection via the existing DI container. Standard idiomatic .NET pattern — no custom abstractions.

## Package & DI Wiring

**New NuGet packages (WPF project only):**
- `Microsoft.Extensions.Logging`
- `Microsoft.Extensions.Logging.Console`

Infrastructure and Application projects need no new packages — `ILogger<T>` is transitively available via EF Core's dependency on `Microsoft.Extensions.Logging.Abstractions`.

**DI registration in `App.ConfigureServices`:**

```csharp
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});
```

**Console allocation for WPF:**

Since WPF is not a console application, allocate a console window in debug builds so the console provider has somewhere to write:

```csharp
#if DEBUG
[DllImport("kernel32.dll")]
private static extern bool AllocConsole();
#endif
```

Called at the top of `OnStartup`, guarded by `#if DEBUG`.

## Where Logging Is Added

### Infrastructure Layer

**`DataImportService`:**
- Info: seed/import start and completion with entity counts
- Error: exceptions during seeding or import

**`BazaarPlannerImporter`:**
- Info: HTTP fetch start per resource (items, skills, monsters)
- Error: HTTP failures, JSON parse failures (replacing the current silent `catch` in `ParseJsArray`)

### Application Layer

**`MonsterEncounterService`:**
- Info: search/lookup calls with parameters
- Warning: monster not found

**`ItemInfoService`:**
- Info: item lookup calls
- Warning: item not found

**`SkillInfoService`:**
- Info: skill lookup calls
- Warning: skill not found

**`ShopService`:**
- Info: shop filtering calls

**`EncounterService`:**
- Info: encounter lookups

**`GameSessionService`:**
- Info: hero/day selection changes

### WPF Layer

**`App`:**
- Info: application startup, DB creation, shutdown
- Error: unhandled exceptions

### Not Logging

- **Domain entities** — no DI, pure logic
- **Repositories** — EF Core already logs queries at Debug level via its own `ILogger` integration
- **ViewModels** — UI actions are visible on screen; logging would be noise

## Log Message Conventions

**Structured parameters** — use `ILogger` placeholders, not string interpolation:

```csharp
// Correct
_logger.LogInformation("Importing {Count} items from BazaarPlanner", items.Count);

// Incorrect
_logger.LogInformation($"Importing {items.Count} items from BazaarPlanner");
```

**Level usage:**
- `LogInformation` — operation start/completion, counts, state changes
- `LogWarning` — expected-but-noteworthy conditions (entity not found, empty results)
- `LogError(exception, ...)` — caught exceptions that were previously swallowed

**No control flow changes.** Logging is observability only. Existing return values and error handling remain unchanged. The one exception: `BazaarPlannerImporter.ParseJsArray`'s bare `catch` block gets the exception logged but still returns an empty list.

## Files Modified

1. `src/BazaarOverlay.WPF/BazaarOverlay.WPF.csproj` — add logging packages
2. `src/BazaarOverlay.WPF/App.xaml.cs` — register logging, allocate debug console
3. `src/BazaarOverlay.Infrastructure/DataImport/DataImportService.cs` — add `ILogger<DataImportService>`
4. `src/BazaarOverlay.Infrastructure/DataImport/BazaarPlannerImporter.cs` — add `ILogger<BazaarPlannerImporter>`
5. `src/BazaarOverlay.Application/Services/MonsterEncounterService.cs` — add `ILogger<MonsterEncounterService>`
6. `src/BazaarOverlay.Application/Services/ItemInfoService.cs` — add `ILogger<ItemInfoService>`
7. `src/BazaarOverlay.Application/Services/SkillInfoService.cs` — add `ILogger<SkillInfoService>`
8. `src/BazaarOverlay.Application/Services/ShopService.cs` — add `ILogger<ShopService>`
9. `src/BazaarOverlay.Application/Services/EncounterService.cs` — add `ILogger<EncounterService>`
10. `src/BazaarOverlay.Application/Services/GameSessionService.cs` — add `ILogger<GameSessionService>`

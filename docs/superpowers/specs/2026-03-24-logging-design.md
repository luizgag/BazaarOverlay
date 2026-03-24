# Logging Design

## Goal

Add development/debugging logging using `Microsoft.Extensions.Logging` with a console provider so developers can see errors and key operations in the console while building and testing.

## Approach

Direct `ILogger<T>` injection via the existing DI container. Standard idiomatic .NET pattern — no custom abstractions.

## Package & DI Wiring

**New NuGet packages (WPF project only):**
- `Microsoft.Extensions.Logging`
- `Microsoft.Extensions.Logging.Console`

**Application project:**
- `Microsoft.Extensions.Logging.Abstractions` — explicit reference needed since Application does not depend on Infrastructure or EF Core (correct per Onion architecture).

Infrastructure needs no new packages — `ILogger<T>` is transitively available via EF Core's dependency on `Microsoft.Extensions.Logging.Abstractions`.

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
- Error: HTTP failures, JSON parse failures
- Note: `ParseJsArray` is a `static` method and cannot access an instance `_logger`. Logging for JSON parse failures moves to the calling instance methods (`FetchItemsAsync`, `FetchSkillsAsync`, `FetchMonstersAsync`) which wrap the `ParseJsArray` call in a try/catch. `ParseJsArray` itself remains static and unchanged (it still throws on error instead of silently returning an empty list — the caller catches and logs).

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

### WPF Layer

**`App`:**
- Info: application startup, DB creation, shutdown
- Error: unhandled exceptions

### Not Logging

- **Domain entities** — no DI, pure logic
- **Repositories** — EF Core already logs queries at Debug level via its own `ILogger` integration
- **ViewModels** — UI actions are visible on screen; logging would be noise
- **`GameSessionService`** — trivial synchronous one-liners (set hero, advance day, reset); state changes are visible in the UI

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

**No control flow changes.** Logging is observability only. Existing return values and error handling remain unchanged.

## Test Convention

Adding `ILogger<T>` to constructors breaks existing tests. Use `NullLogger<T>.Instance` (from `Microsoft.Extensions.Logging.Abstractions`) in tests — we don't assert on log output.

The test project may need an explicit `Microsoft.Extensions.Logging.Abstractions` package reference if it's not already transitively available.

## Files Modified

### Source
1. `src/BazaarOverlay.WPF/BazaarOverlay.WPF.csproj` — add logging packages
2. `src/BazaarOverlay.WPF/App.xaml.cs` — register logging, allocate debug console
3. `src/BazaarOverlay.Application/BazaarOverlay.Application.csproj` — add `Microsoft.Extensions.Logging.Abstractions` package
4. `src/BazaarOverlay.Infrastructure/DataImport/DataImportService.cs` — add `ILogger<DataImportService>`
5. `src/BazaarOverlay.Infrastructure/DataImport/BazaarPlannerImporter.cs` — add `ILogger<BazaarPlannerImporter>`
6. `src/BazaarOverlay.Application/Services/MonsterEncounterService.cs` — add `ILogger<MonsterEncounterService>`
7. `src/BazaarOverlay.Application/Services/ItemInfoService.cs` — add `ILogger<ItemInfoService>`
8. `src/BazaarOverlay.Application/Services/SkillInfoService.cs` — add `ILogger<SkillInfoService>`
9. `src/BazaarOverlay.Application/Services/ShopService.cs` — add `ILogger<ShopService>`
10. `src/BazaarOverlay.Application/Services/EncounterService.cs` — add `ILogger<EncounterService>`

### Tests
11. `tests/BazaarOverlay.Tests/Application/MonsterEncounterServiceTests.cs` — pass `NullLogger<T>.Instance`
12. `tests/BazaarOverlay.Tests/Application/ItemInfoServiceTests.cs` — pass `NullLogger<T>.Instance`
13. `tests/BazaarOverlay.Tests/Application/SkillInfoServiceTests.cs` — pass `NullLogger<T>.Instance`
14. `tests/BazaarOverlay.Tests/Application/ShopServiceTests.cs` — pass `NullLogger<T>.Instance`
15. `tests/BazaarOverlay.Tests/Application/EncounterServiceTests.cs` — pass `NullLogger<T>.Instance`
16. `tests/BazaarOverlay.Tests/Infrastructure/DataImportTests.cs` — pass `NullLogger<T>.Instance`

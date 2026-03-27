# BazaarDB Live Overlay Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a Ctrl+D hotkey that screenshots the area around the cursor, OCRs the tooltip to extract entity names, looks them up on bazaardb.gg via Playwright, and displays the card page in a WebView2 overlay window.

**Architecture:** Three additive subsystems — Screen Capture & OCR, BazaarDb Lookup & Caching, and Overlay Display. All new code follows the existing Onion/DDD architecture. Open/Closed principle: extend only, no modifications to existing code. Interfaces in Application/Domain layers, implementations in Infrastructure/WPF layers.

**Tech Stack:** .NET 10, WPF, EF Core + SQLite, Microsoft.Playwright, Microsoft.Web.WebView2, Windows.Media.Ocr (WinRT), CommunityToolkit.Mvvm, xUnit + Shouldly + NSubstitute

---

## File Structure

### Domain Layer (`src/BazaarOverlay.Domain/`)
| File | Responsibility |
|------|---------------|
| `Entities/CardUrlCache.cs` | Cache entity: Name → CardUrl mapping |
| `Interfaces/ICardUrlCacheRepository.cs` | Repository interface for cache CRUD |

### Application Layer (`src/BazaarOverlay.Application/`)
| File | Responsibility |
|------|---------------|
| `Interfaces/IScreenCaptureService.cs` | Captures bitmap region around cursor |
| `Interfaces/IOcrService.cs` | Extracts text lines from bitmap |
| `Interfaces/ITooltipNameExtractor.cs` | Picks entity name from OCR text lines |
| `Interfaces/IBazaarDbLookupService.cs` | Orchestrates cache + Playwright search |
| `Interfaces/IOverlayOrchestrator.cs` | Ties hotkey → capture → OCR → lookup → display |

### Infrastructure Layer (`src/BazaarOverlay.Infrastructure/`)
| File | Responsibility |
|------|---------------|
| `Persistence/Repositories/CardUrlCacheRepository.cs` | SQLite cache repository |
| `ScreenCapture/ScreenCaptureService.cs` | P/Invoke screen capture |
| `Ocr/WindowsOcrService.cs` | WinRT OCR implementation |
| `Ocr/TooltipNameExtractor.cs` | Name extraction heuristics |
| `Playwright/IPlaywrightBrowserManager.cs` | Browser lifecycle interface (infra-internal) |
| `Playwright/IPlaywrightSearchService.cs` | Search interface (infra-internal) |
| `Playwright/PlaywrightBrowserManager.cs` | Singleton Chromium browser instance |
| `Playwright/PlaywrightSearchService.cs` | bazaardb.gg search + result scraping |
| `Playwright/BazaarDbLookupService.cs` | Cache-then-search orchestration |

### WPF Layer (`src/BazaarOverlay.WPF/`)
| File | Responsibility |
|------|---------------|
| `ViewModels/CardOverlayViewModel.cs` | CardUrl, IsVisible, ShowCard(), Hide() |
| `Views/CardOverlayWindow.xaml` | Borderless topmost WebView2 window |
| `Views/CardOverlayWindow.xaml.cs` | Code-behind for overlay window |
| `Services/OverlayOrchestrator.cs` | Implements IOverlayOrchestrator, coordinates flow |
| `Services/HotkeyService.cs` | Global Ctrl+D hotkey via RegisterHotKey P/Invoke |

### Tests (`tests/BazaarOverlay.Tests/`)
| File | Responsibility |
|------|---------------|
| `Domain/CardUrlCacheTests.cs` | Entity construction and validation |
| `Infrastructure/CardUrlCacheRepositoryTests.cs` | Repository with in-memory SQLite |
| `Infrastructure/TooltipNameExtractorTests.cs` | Name extraction heuristics |
| `Application/BazaarDbLookupServiceTests.cs` | Lookup orchestration with mocks |
| `WPF/CardOverlayViewModelTests.cs` | ViewModel state transitions |
| `WPF/OverlayOrchestratorTests.cs` | Orchestrator with mocked services |

---

## Task 1: CardUrlCache Domain Entity

**Files:**
- Create: `src/BazaarOverlay.Domain/Entities/CardUrlCache.cs`
- Create: `tests/BazaarOverlay.Tests/Domain/CardUrlCacheTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/BazaarOverlay.Tests/Domain/CardUrlCacheTests.cs
using BazaarOverlay.Domain.Entities;
using Shouldly;

namespace BazaarOverlay.Tests.Domain;

public class CardUrlCacheTests
{
    [Fact]
    public void Constructor_ValidData_SetsProperties()
    {
        var cache = new CardUrlCache("Pigomorph", "/card/123/pigomorph", "Item");

        cache.Name.ShouldBe("Pigomorph");
        cache.CardUrl.ShouldBe("/card/123/pigomorph");
        cache.Category.ShouldBe("Item");
        cache.CachedAt.ShouldBeInRange(DateTime.UtcNow.AddSeconds(-5), DateTime.UtcNow);
    }

    [Fact]
    public void Constructor_TrimsWhitespace()
    {
        var cache = new CardUrlCache("  Pigomorph  ", "/card/123/pigomorph ", " Item ");

        cache.Name.ShouldBe("Pigomorph");
        cache.CardUrl.ShouldBe("/card/123/pigomorph");
        cache.Category.ShouldBe("Item");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyName_Throws(string? name)
    {
        Should.Throw<ArgumentException>(() => new CardUrlCache(name!, "/card/123/pigomorph", "Item"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyCardUrl_Throws(string? cardUrl)
    {
        Should.Throw<ArgumentException>(() => new CardUrlCache("Pigomorph", cardUrl!, "Item"));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/BazaarOverlay.Tests --filter "FullyQualifiedName~CardUrlCacheTests" --no-restore -v minimal`
Expected: Compilation error — `CardUrlCache` does not exist.

- [ ] **Step 3: Write the entity**

```csharp
// src/BazaarOverlay.Domain/Entities/CardUrlCache.cs
using System.ComponentModel.DataAnnotations;

namespace BazaarOverlay.Domain.Entities;

public class CardUrlCache
{
    [Key]
    public int Id { get; private set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; private set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string CardUrl { get; private set; } = string.Empty;

    [MaxLength(50)]
    public string? Category { get; private set; }

    public DateTime CachedAt { get; private set; }

    private CardUrlCache() { }

    public CardUrlCache(string name, string cardUrl, string? category = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Cache entry name cannot be empty.", nameof(name));
        if (string.IsNullOrWhiteSpace(cardUrl))
            throw new ArgumentException("Card URL cannot be empty.", nameof(cardUrl));

        Name = name.Trim();
        CardUrl = cardUrl.Trim();
        Category = category?.Trim();
        CachedAt = DateTime.UtcNow;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/BazaarOverlay.Tests --filter "FullyQualifiedName~CardUrlCacheTests" --no-restore -v minimal`
Expected: All 4 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/BazaarOverlay.Domain/Entities/CardUrlCache.cs tests/BazaarOverlay.Tests/Domain/CardUrlCacheTests.cs
git commit -m "feat: add CardUrlCache domain entity"
```

---

## Task 2: ICardUrlCacheRepository Interface

**Files:**
- Create: `src/BazaarOverlay.Domain/Interfaces/ICardUrlCacheRepository.cs`

- [ ] **Step 1: Create the repository interface**

```csharp
// src/BazaarOverlay.Domain/Interfaces/ICardUrlCacheRepository.cs
using BazaarOverlay.Domain.Entities;

namespace BazaarOverlay.Domain.Interfaces;

public interface ICardUrlCacheRepository
{
    Task<CardUrlCache?> GetByNameAsync(string name);
    Task SaveAsync(CardUrlCache entry);
}
```

- [ ] **Step 2: Verify solution builds**

Run: `dotnet build src/BazaarOverlay.Domain --no-restore -v minimal`
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add src/BazaarOverlay.Domain/Interfaces/ICardUrlCacheRepository.cs
git commit -m "feat: add ICardUrlCacheRepository interface"
```

---

## Task 3: CardUrlCacheRepository + DbContext Update

**Files:**
- Create: `src/BazaarOverlay.Infrastructure/Persistence/Repositories/CardUrlCacheRepository.cs`
- Modify: `src/BazaarOverlay.Infrastructure/Persistence/BazaarDbContext.cs` (add DbSet + fluent config)
- Create: `tests/BazaarOverlay.Tests/Infrastructure/CardUrlCacheRepositoryTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/BazaarOverlay.Tests/Infrastructure/CardUrlCacheRepositoryTests.cs
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Infrastructure.Persistence.Repositories;
using BazaarOverlay.Tests.Infrastructure;
using Shouldly;

namespace BazaarOverlay.Tests.Infrastructure;

public class CardUrlCacheRepositoryTests : IDisposable
{
    private readonly BazaarOverlay.Infrastructure.Persistence.BazaarDbContext _context;
    private readonly CardUrlCacheRepository _repository;

    public CardUrlCacheRepositoryTests()
    {
        _context = TestDbContextFactory.Create();
        _repository = new CardUrlCacheRepository(_context);
    }

    [Fact]
    public async Task GetByNameAsync_ExistingEntry_ReturnsEntry()
    {
        _context.CardUrlCaches.Add(new CardUrlCache("Pigomorph", "/card/123/pigomorph", "Item"));
        await _context.SaveChangesAsync();

        var result = await _repository.GetByNameAsync("Pigomorph");

        result.ShouldNotBeNull();
        result.Name.ShouldBe("Pigomorph");
        result.CardUrl.ShouldBe("/card/123/pigomorph");
    }

    [Fact]
    public async Task GetByNameAsync_CaseInsensitive_ReturnsEntry()
    {
        _context.CardUrlCaches.Add(new CardUrlCache("Pigomorph", "/card/123/pigomorph", "Item"));
        await _context.SaveChangesAsync();

        var result = await _repository.GetByNameAsync("pigomorph");

        result.ShouldNotBeNull();
        result.Name.ShouldBe("Pigomorph");
    }

    [Fact]
    public async Task GetByNameAsync_NotFound_ReturnsNull()
    {
        var result = await _repository.GetByNameAsync("NonExistent");

        result.ShouldBeNull();
    }

    [Fact]
    public async Task SaveAsync_NewEntry_PersistsToDatabase()
    {
        var entry = new CardUrlCache("Pigomorph", "/card/123/pigomorph", "Item");

        await _repository.SaveAsync(entry);

        var saved = await _context.CardUrlCaches.FindAsync(entry.Id);
        saved.ShouldNotBeNull();
        saved.Name.ShouldBe("Pigomorph");
    }

    public void Dispose() => _context.Dispose();
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/BazaarOverlay.Tests --filter "FullyQualifiedName~CardUrlCacheRepositoryTests" --no-restore -v minimal`
Expected: Compilation error — `CardUrlCacheRepository` and `CardUrlCaches` DbSet do not exist.

- [ ] **Step 3: Add DbSet and fluent configuration to BazaarDbContext**

Add to `BazaarDbContext.cs`:
- New DbSet property: `public DbSet<CardUrlCache> CardUrlCaches => Set<CardUrlCache>();`
- New fluent config in `OnModelCreating`:

```csharp
modelBuilder.Entity<CardUrlCache>(entity =>
{
    entity.HasIndex(e => e.Name).IsUnique();
});
```

- [ ] **Step 4: Write the repository implementation**

```csharp
// src/BazaarOverlay.Infrastructure/Persistence/Repositories/CardUrlCacheRepository.cs
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BazaarOverlay.Infrastructure.Persistence.Repositories;

public class CardUrlCacheRepository : ICardUrlCacheRepository
{
    private readonly BazaarDbContext _context;

    public CardUrlCacheRepository(BazaarDbContext context)
    {
        _context = context;
    }

    public async Task<CardUrlCache?> GetByNameAsync(string name)
    {
        return await _context.CardUrlCaches
            .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());
    }

    public async Task SaveAsync(CardUrlCache entry)
    {
        await _context.CardUrlCaches.AddAsync(entry);
        await _context.SaveChangesAsync();
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test tests/BazaarOverlay.Tests --filter "FullyQualifiedName~CardUrlCacheRepositoryTests" --no-restore -v minimal`
Expected: All 4 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add src/BazaarOverlay.Infrastructure/Persistence/Repositories/CardUrlCacheRepository.cs src/BazaarOverlay.Infrastructure/Persistence/BazaarDbContext.cs tests/BazaarOverlay.Tests/Infrastructure/CardUrlCacheRepositoryTests.cs
git commit -m "feat: add CardUrlCacheRepository with SQLite persistence"
```

---

## Task 4: TooltipNameExtractor

**Files:**
- Create: `src/BazaarOverlay.Application/Interfaces/ITooltipNameExtractor.cs`
- Create: `src/BazaarOverlay.Infrastructure/Ocr/TooltipNameExtractor.cs`
- Create: `tests/BazaarOverlay.Tests/Infrastructure/TooltipNameExtractorTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/BazaarOverlay.Tests/Infrastructure/TooltipNameExtractorTests.cs
using BazaarOverlay.Infrastructure.Ocr;
using Shouldly;

namespace BazaarOverlay.Tests.Infrastructure;

public class TooltipNameExtractorTests
{
    private readonly TooltipNameExtractor _extractor = new();

    [Fact]
    public void ExtractName_SingleLine_ReturnsLine()
    {
        var lines = new[] { "Pigomorph" };

        var result = _extractor.ExtractName(lines);

        result.ShouldBe("Pigomorph");
    }

    [Fact]
    public void ExtractName_MultipleLines_ReturnsFirstNonEmpty()
    {
        var lines = new[] { "Pigomorph", "Tier: Gold", "+50% damage to Monsters" };

        var result = _extractor.ExtractName(lines);

        result.ShouldBe("Pigomorph");
    }

    [Fact]
    public void ExtractName_FirstLineEmpty_SkipsToNext()
    {
        var lines = new[] { "", "  ", "Pigomorph", "Some description" };

        var result = _extractor.ExtractName(lines);

        result.ShouldBe("Pigomorph");
    }

    [Fact]
    public void ExtractName_EmptyArray_ReturnsNull()
    {
        var lines = Array.Empty<string>();

        var result = _extractor.ExtractName(lines);

        result.ShouldBeNull();
    }

    [Fact]
    public void ExtractName_AllEmptyLines_ReturnsNull()
    {
        var lines = new[] { "", "  ", "   " };

        var result = _extractor.ExtractName(lines);

        result.ShouldBeNull();
    }

    [Fact]
    public void ExtractName_TrimsWhitespace()
    {
        var lines = new[] { "  Pigomorph  " };

        var result = _extractor.ExtractName(lines);

        result.ShouldBe("Pigomorph");
    }

}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/BazaarOverlay.Tests --filter "FullyQualifiedName~TooltipNameExtractorTests" --no-restore -v minimal`
Expected: Compilation error — `TooltipNameExtractor` does not exist.

- [ ] **Step 3: Create the interface**

```csharp
// src/BazaarOverlay.Application/Interfaces/ITooltipNameExtractor.cs
namespace BazaarOverlay.Application.Interfaces;

public interface ITooltipNameExtractor
{
    string? ExtractName(IReadOnlyList<string> ocrLines);
}
```

- [ ] **Step 4: Write the implementation**

```csharp
// src/BazaarOverlay.Infrastructure/Ocr/TooltipNameExtractor.cs
using BazaarOverlay.Application.Interfaces;

namespace BazaarOverlay.Infrastructure.Ocr;

public class TooltipNameExtractor : ITooltipNameExtractor
{
    public string? ExtractName(IReadOnlyList<string> ocrLines)
    {
        foreach (var line in ocrLines)
        {
            var trimmed = line.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed))
                return trimmed;
        }

        return null;
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test tests/BazaarOverlay.Tests --filter "FullyQualifiedName~TooltipNameExtractorTests" --no-restore -v minimal`
Expected: All 6 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add src/BazaarOverlay.Application/Interfaces/ITooltipNameExtractor.cs src/BazaarOverlay.Infrastructure/Ocr/TooltipNameExtractor.cs tests/BazaarOverlay.Tests/Infrastructure/TooltipNameExtractorTests.cs
git commit -m "feat: add TooltipNameExtractor returning first non-empty OCR line"
```

---

## Task 5: Screen Capture & OCR Interfaces + Implementations

**Files:**
- Create: `src/BazaarOverlay.Application/Interfaces/IScreenCaptureService.cs`
- Create: `src/BazaarOverlay.Application/Interfaces/IOcrService.cs`
- Create: `src/BazaarOverlay.Infrastructure/ScreenCapture/ScreenCaptureService.cs`
- Create: `src/BazaarOverlay.Infrastructure/Ocr/WindowsOcrService.cs`
- Modify: `src/BazaarOverlay.Infrastructure/BazaarOverlay.Infrastructure.csproj` (add Windows TFM)

These implementations use Windows-specific APIs (P/Invoke, WinRT) and cannot be meaningfully unit tested. They will be tested via manual integration testing.

- [ ] **Step 1: Create the IScreenCaptureService interface**

```csharp
// src/BazaarOverlay.Application/Interfaces/IScreenCaptureService.cs
namespace BazaarOverlay.Application.Interfaces;

public interface IScreenCaptureService
{
    /// <summary>
    /// Gets the current cursor position in screen coordinates.
    /// </summary>
    (int X, int Y) GetCursorPosition();

    /// <summary>
    /// Captures a region of the screen as a byte array (PNG format).
    /// </summary>
    byte[] CaptureRegion(int x, int y, int width, int height);
}
```

- [ ] **Step 2: Create the IOcrService interface**

```csharp
// src/BazaarOverlay.Application/Interfaces/IOcrService.cs
namespace BazaarOverlay.Application.Interfaces;

public interface IOcrService
{
    /// <summary>
    /// Extracts text lines from a bitmap image (PNG format byte array).
    /// </summary>
    Task<IReadOnlyList<string>> RecognizeTextAsync(byte[] imageData);
}
```

- [ ] **Step 3: Update Infrastructure .csproj for Windows TFM**

Change `src/BazaarOverlay.Infrastructure/BazaarOverlay.Infrastructure.csproj` `<TargetFramework>` from `net10.0` to `net10.0-windows10.0.22621.0` and add `<EnableWindowsTargeting>true</EnableWindowsTargeting>`:

```xml
<PropertyGroup>
    <TargetFramework>net10.0-windows10.0.22621.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <EnableWindowsTargeting>true</EnableWindowsTargeting>
</PropertyGroup>
```

- [ ] **Step 4: Write the ScreenCaptureService implementation**

```csharp
// src/BazaarOverlay.Infrastructure/ScreenCapture/ScreenCaptureService.cs
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using BazaarOverlay.Application.Interfaces;

namespace BazaarOverlay.Infrastructure.ScreenCapture;

public partial class ScreenCaptureService : IScreenCaptureService
{
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    public (int X, int Y) GetCursorPosition()
    {
        GetCursorPos(out var point);
        return (point.X, point.Y);
    }

    public byte[] CaptureRegion(int x, int y, int width, int height)
    {
        using var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(x, y, 0, 0, new Size(width, height));

        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }
}
```

- [ ] **Step 5: Write the WindowsOcrService implementation**

```csharp
// src/BazaarOverlay.Infrastructure/Ocr/WindowsOcrService.cs
using System.Runtime.InteropServices;
using BazaarOverlay.Application.Interfaces;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace BazaarOverlay.Infrastructure.Ocr;

public class WindowsOcrService : IOcrService
{
    public async Task<IReadOnlyList<string>> RecognizeTextAsync(byte[] imageData)
    {
        var ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();
        if (ocrEngine is null)
            return Array.Empty<string>();

        using var stream = new InMemoryRandomAccessStream();
        await stream.WriteAsync(imageData.AsBuffer());
        stream.Seek(0);

        var decoder = await BitmapDecoder.CreateAsync(stream);
        var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

        var result = await ocrEngine.RecognizeAsync(softwareBitmap);

        return result.Lines.Select(line => line.Text).ToList();
    }
}
```

- [ ] **Step 6: Verify solution builds**

Run: `dotnet build BazaarOverlay.slnx --no-restore -v minimal`
Expected: Build succeeded.

- [ ] **Step 7: Commit**

```bash
git add src/BazaarOverlay.Application/Interfaces/IScreenCaptureService.cs src/BazaarOverlay.Application/Interfaces/IOcrService.cs src/BazaarOverlay.Infrastructure/ScreenCapture/ScreenCaptureService.cs src/BazaarOverlay.Infrastructure/Ocr/WindowsOcrService.cs src/BazaarOverlay.Infrastructure/BazaarOverlay.Infrastructure.csproj
git commit -m "feat: add ScreenCaptureService and WindowsOcrService implementations"
```

---

## Task 6: Playwright Browser Manager & Search Service

**Files:**
- Create: `src/BazaarOverlay.Infrastructure/Playwright/IPlaywrightBrowserManager.cs`
- Create: `src/BazaarOverlay.Infrastructure/Playwright/IPlaywrightSearchService.cs`
- Create: `src/BazaarOverlay.Infrastructure/Playwright/PlaywrightBrowserManager.cs`
- Create: `src/BazaarOverlay.Infrastructure/Playwright/PlaywrightSearchService.cs`
- Modify: `src/BazaarOverlay.Infrastructure/BazaarOverlay.Infrastructure.csproj` (add Microsoft.Playwright)

These are integration-level services (headless browser). Unit testing them is impractical — they will be tested via the BazaarDbLookupService (Task 7) using mocks of these interfaces.

- [ ] **Step 1: Add Microsoft.Playwright NuGet package**

Run: `dotnet add src/BazaarOverlay.Infrastructure/BazaarOverlay.Infrastructure.csproj package Microsoft.Playwright`

- [ ] **Step 2: Create IPlaywrightBrowserManager interface**

```csharp
// src/BazaarOverlay.Infrastructure/Playwright/IPlaywrightBrowserManager.cs
using Microsoft.Playwright;

namespace BazaarOverlay.Infrastructure.Playwright;

public interface IPlaywrightBrowserManager : IAsyncDisposable
{
    Task<IBrowserContext> GetBrowserContextAsync();
}
```

- [ ] **Step 3: Create IPlaywrightSearchService interface**

```csharp
// src/BazaarOverlay.Infrastructure/Playwright/IPlaywrightSearchService.cs
namespace BazaarOverlay.Infrastructure.Playwright;

public interface IPlaywrightSearchService
{
    /// <summary>
    /// Searches bazaardb.gg for the given name and returns the card URL path (e.g., "/card/123/pigomorph"), or null if not found.
    /// </summary>
    Task<(string? CardUrl, string? Category)> SearchAsync(string name);
}
```

- [ ] **Step 4: Write PlaywrightBrowserManager implementation**

```csharp
// src/BazaarOverlay.Infrastructure/Playwright/PlaywrightBrowserManager.cs
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace BazaarOverlay.Infrastructure.Playwright;

public class PlaywrightBrowserManager : IPlaywrightBrowserManager
{
    private readonly ILogger<PlaywrightBrowserManager> _logger;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public PlaywrightBrowserManager(ILogger<PlaywrightBrowserManager> logger)
    {
        _logger = logger;
    }

    public async Task<IBrowserContext> GetBrowserContextAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (_context is not null)
                return _context;

            _logger.LogInformation("Launching headless Playwright Chromium...");
            _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });
            _context = await _browser.NewContextAsync();
            _logger.LogInformation("Playwright browser context ready");
            return _context;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_context is not null) await _context.DisposeAsync();
        if (_browser is not null) await _browser.DisposeAsync();
        _playwright?.Dispose();
        _lock.Dispose();
        GC.SuppressFinalize(this);
    }
}
```

- [ ] **Step 5: Write PlaywrightSearchService implementation**

```csharp
// src/BazaarOverlay.Infrastructure/Playwright/PlaywrightSearchService.cs
using System.Web;
using Microsoft.Extensions.Logging;

namespace BazaarOverlay.Infrastructure.Playwright;

public class PlaywrightSearchService : IPlaywrightSearchService
{
    private readonly IPlaywrightBrowserManager _browserManager;
    private readonly ILogger<PlaywrightSearchService> _logger;

    public PlaywrightSearchService(
        IPlaywrightBrowserManager browserManager,
        ILogger<PlaywrightSearchService> logger)
    {
        _browserManager = browserManager;
        _logger = logger;
    }

    public async Task<(string? CardUrl, string? Category)> SearchAsync(string name)
    {
        var context = await _browserManager.GetBrowserContextAsync();
        var page = await context.NewPageAsync();

        try
        {
            var encodedName = HttpUtility.UrlEncode(name);
            var searchUrl = $"https://bazaardb.gg/search?q={encodedName}";
            _logger.LogInformation("Searching bazaardb.gg for: {Name}", name);

            await page.GotoAsync(searchUrl, new() { WaitUntil = Microsoft.Playwright.WaitUntilState.NetworkIdle });

            // Wait for search results to render
            var firstResult = await page.QuerySelectorAsync("a[href^='/items/'], a[href^='/skills/'], a[href^='/monsters/'], a[href^='/encounters/']");
            if (firstResult is null)
            {
                _logger.LogWarning("No search results found for: {Name}", name);
                return (null, null);
            }

            var href = await firstResult.GetAttributeAsync("href");
            if (string.IsNullOrEmpty(href))
                return (null, null);

            // Determine category from URL path
            var category = href.Split('/').ElementAtOrDefault(1);

            var fullUrl = $"https://bazaardb.gg{href}";
            _logger.LogInformation("Found card URL: {Url} (category: {Category})", fullUrl, category);
            return (fullUrl, category);
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}
```

- [ ] **Step 6: Verify solution builds**

Run: `dotnet build src/BazaarOverlay.Infrastructure --no-restore -v minimal`
Expected: Build succeeded.

- [ ] **Step 7: Commit**

```bash
git add src/BazaarOverlay.Infrastructure/Playwright/ src/BazaarOverlay.Infrastructure/BazaarOverlay.Infrastructure.csproj
git commit -m "feat: add PlaywrightBrowserManager and PlaywrightSearchService"
```

---

## Task 7: BazaarDbLookupService

**Files:**
- Create: `src/BazaarOverlay.Application/Interfaces/IBazaarDbLookupService.cs`
- Create: `src/BazaarOverlay.Infrastructure/Playwright/BazaarDbLookupService.cs`
- Create: `tests/BazaarOverlay.Tests/Application/BazaarDbLookupServiceTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/BazaarOverlay.Tests/Application/BazaarDbLookupServiceTests.cs
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Interfaces;
using BazaarOverlay.Infrastructure.Playwright;
using NSubstitute;
using Shouldly;

namespace BazaarOverlay.Tests.Application;

public class BazaarDbLookupServiceTests
{
    private readonly ICardUrlCacheRepository _cacheRepo = Substitute.For<ICardUrlCacheRepository>();
    private readonly IPlaywrightSearchService _searchService = Substitute.For<IPlaywrightSearchService>();
    private readonly BazaarDbLookupService _service;

    public BazaarDbLookupServiceTests()
    {
        _service = new BazaarDbLookupService(_cacheRepo, _searchService);
    }

    [Fact]
    public async Task GetCardUrlAsync_CacheHit_ReturnsCachedUrl()
    {
        var cached = new CardUrlCache("Pigomorph", "https://bazaardb.gg/items/pigomorph", "items");
        _cacheRepo.GetByNameAsync("Pigomorph").Returns(cached);

        var result = await _service.GetCardUrlAsync("Pigomorph");

        result.ShouldBe("https://bazaardb.gg/items/pigomorph");
        await _searchService.DidNotReceive().SearchAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task GetCardUrlAsync_CacheMiss_SearchesAndCaches()
    {
        _cacheRepo.GetByNameAsync("Pigomorph").Returns((CardUrlCache?)null);
        _searchService.SearchAsync("Pigomorph").Returns(("https://bazaardb.gg/items/pigomorph", "items"));

        var result = await _service.GetCardUrlAsync("Pigomorph");

        result.ShouldBe("https://bazaardb.gg/items/pigomorph");
        await _cacheRepo.Received(1).SaveAsync(Arg.Is<CardUrlCache>(c =>
            c.Name == "Pigomorph" &&
            c.CardUrl == "https://bazaardb.gg/items/pigomorph" &&
            c.Category == "items"));
    }

    [Fact]
    public async Task GetCardUrlAsync_SearchReturnsNull_ReturnsNull()
    {
        _cacheRepo.GetByNameAsync("Unknown").Returns((CardUrlCache?)null);
        _searchService.SearchAsync("Unknown").Returns(((string?)null, (string?)null));

        var result = await _service.GetCardUrlAsync("Unknown");

        result.ShouldBeNull();
        await _cacheRepo.DidNotReceive().SaveAsync(Arg.Any<CardUrlCache>());
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/BazaarOverlay.Tests --filter "FullyQualifiedName~BazaarDbLookupServiceTests" --no-restore -v minimal`
Expected: Compilation error — `IBazaarDbLookupService` and `BazaarDbLookupService` do not exist.

- [ ] **Step 3: Create the interface**

```csharp
// src/BazaarOverlay.Application/Interfaces/IBazaarDbLookupService.cs
namespace BazaarOverlay.Application.Interfaces;

public interface IBazaarDbLookupService
{
    /// <summary>
    /// Looks up a card URL on bazaardb.gg by entity name. Uses cache first, then Playwright search.
    /// Returns the full card page URL, or null if not found.
    /// </summary>
    Task<string?> GetCardUrlAsync(string name);
}
```

- [ ] **Step 4: Write the implementation**

```csharp
// src/BazaarOverlay.Infrastructure/Playwright/BazaarDbLookupService.cs
using BazaarOverlay.Application.Interfaces;
using BazaarOverlay.Domain.Entities;
using BazaarOverlay.Domain.Interfaces;

namespace BazaarOverlay.Infrastructure.Playwright;

public class BazaarDbLookupService : IBazaarDbLookupService
{
    private readonly ICardUrlCacheRepository _cacheRepository;
    private readonly IPlaywrightSearchService _searchService;

    public BazaarDbLookupService(
        ICardUrlCacheRepository cacheRepository,
        IPlaywrightSearchService searchService)
    {
        _cacheRepository = cacheRepository;
        _searchService = searchService;
    }

    public async Task<string?> GetCardUrlAsync(string name)
    {
        var cached = await _cacheRepository.GetByNameAsync(name);
        if (cached is not null)
            return cached.CardUrl;

        var (cardUrl, category) = await _searchService.SearchAsync(name);
        if (cardUrl is null)
            return null;

        var entry = new CardUrlCache(name, cardUrl, category);
        await _cacheRepository.SaveAsync(entry);

        return cardUrl;
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test tests/BazaarOverlay.Tests --filter "FullyQualifiedName~BazaarDbLookupServiceTests" --no-restore -v minimal`
Expected: All 3 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add src/BazaarOverlay.Application/Interfaces/IBazaarDbLookupService.cs src/BazaarOverlay.Infrastructure/Playwright/BazaarDbLookupService.cs tests/BazaarOverlay.Tests/Application/BazaarDbLookupServiceTests.cs
git commit -m "feat: add BazaarDbLookupService with cache-then-search flow"
```

---

## Task 8: CardOverlayViewModel

**Files:**
- Create: `src/BazaarOverlay.WPF/ViewModels/CardOverlayViewModel.cs`
- Create: `tests/BazaarOverlay.Tests/WPF/CardOverlayViewModelTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/BazaarOverlay.Tests/WPF/CardOverlayViewModelTests.cs
using BazaarOverlay.WPF.ViewModels;
using Shouldly;

namespace BazaarOverlay.Tests.WPF;

public class CardOverlayViewModelTests
{
    private readonly CardOverlayViewModel _vm = new();

    [Fact]
    public void InitialState_IsNotVisible()
    {
        _vm.IsVisible.ShouldBeFalse();
        _vm.CardUrl.ShouldBeNull();
    }

    [Fact]
    public void ShowCard_SetsUrlAndVisibility()
    {
        _vm.ShowCard("https://bazaardb.gg/items/pigomorph", 100, 200);

        _vm.IsVisible.ShouldBeTrue();
        _vm.CardUrl.ShouldBe("https://bazaardb.gg/items/pigomorph");
        _vm.Left.ShouldBe(100);
        _vm.Top.ShouldBe(200);
    }

    [Fact]
    public void Hide_ClearsVisibility()
    {
        _vm.ShowCard("https://bazaardb.gg/items/pigomorph", 100, 200);
        _vm.Hide();

        _vm.IsVisible.ShouldBeFalse();
    }

    [Fact]
    public void ShowCard_WhileAlreadyShowing_UpdatesUrl()
    {
        _vm.ShowCard("https://bazaardb.gg/items/pigomorph", 100, 200);
        _vm.ShowCard("https://bazaardb.gg/skills/fireball", 300, 400);

        _vm.IsVisible.ShouldBeTrue();
        _vm.CardUrl.ShouldBe("https://bazaardb.gg/skills/fireball");
        _vm.Left.ShouldBe(300);
        _vm.Top.ShouldBe(400);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/BazaarOverlay.Tests --filter "FullyQualifiedName~CardOverlayViewModelTests" --no-restore -v minimal`
Expected: Compilation error — `CardOverlayViewModel` does not exist.

- [ ] **Step 3: Write the ViewModel**

```csharp
// src/BazaarOverlay.WPF/ViewModels/CardOverlayViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;

namespace BazaarOverlay.WPF.ViewModels;

public partial class CardOverlayViewModel : ObservableObject
{
    [ObservableProperty]
    private string? _cardUrl;

    [ObservableProperty]
    private bool _isVisible;

    [ObservableProperty]
    private double _left;

    [ObservableProperty]
    private double _top;

    public void ShowCard(string url, double left, double top)
    {
        CardUrl = url;
        Left = left;
        Top = top;
        IsVisible = true;
    }

    public void Hide()
    {
        IsVisible = false;
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/BazaarOverlay.Tests --filter "FullyQualifiedName~CardOverlayViewModelTests" --no-restore -v minimal`
Expected: All 4 tests PASS.

- [ ] **Step 5: Commit**

```bash
git add src/BazaarOverlay.WPF/ViewModels/CardOverlayViewModel.cs tests/BazaarOverlay.Tests/WPF/CardOverlayViewModelTests.cs
git commit -m "feat: add CardOverlayViewModel with show/hide state"
```

---

## Task 9: CardOverlayWindow (WebView2)

**Files:**
- Create: `src/BazaarOverlay.WPF/Views/CardOverlayWindow.xaml`
- Create: `src/BazaarOverlay.WPF/Views/CardOverlayWindow.xaml.cs`
- Modify: `src/BazaarOverlay.WPF/BazaarOverlay.WPF.csproj` (add WebView2 package)

This is a UI component — tested manually.

- [ ] **Step 1: Add Microsoft.Web.WebView2 NuGet package**

Run: `dotnet add src/BazaarOverlay.WPF/BazaarOverlay.WPF.csproj package Microsoft.Web.WebView2`

- [ ] **Step 2: Create the XAML window**

```xml
<!-- src/BazaarOverlay.WPF/Views/CardOverlayWindow.xaml -->
<Window x:Class="BazaarOverlay.WPF.Views.CardOverlayWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:wv2="clr-namespace:Microsoft.Web.WebView2.Wpf;assembly=Microsoft.Web.WebView2.Wpf"
        Title="BazaarDB Card"
        Width="450" Height="700"
        WindowStyle="None"
        AllowsTransparency="True"
        Topmost="True"
        ShowInTaskbar="False"
        ResizeMode="NoResize"
        Background="Transparent">
    <Border CornerRadius="8" Background="#FF1E1E2E" BorderBrush="#FF444466" BorderThickness="1">
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="Auto"/>
                <RowDefinition Height="*"/>
            </Grid.RowDefinitions>

            <!-- Drag handle + close button -->
            <Grid Grid.Row="0" Background="#FF2A2A3E" MouseLeftButtonDown="DragBar_MouseLeftButtonDown">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>
                <TextBlock Text="BazaarDB" Foreground="#FFCCCCDD" Margin="10,5" VerticalAlignment="Center" FontSize="12"/>
                <Button Grid.Column="1" Content="X" Width="30" Height="24" Margin="4"
                        Click="CloseButton_Click"
                        Background="Transparent" Foreground="#FFCCCCDD" BorderThickness="0"
                        FontWeight="Bold" Cursor="Hand"/>
            </Grid>

            <!-- WebView2 control -->
            <wv2:WebView2 x:Name="WebView" Grid.Row="1"/>
        </Grid>
    </Border>
</Window>
```

- [ ] **Step 3: Create the code-behind**

```csharp
// src/BazaarOverlay.WPF/Views/CardOverlayWindow.xaml.cs
using System.Windows;
using System.Windows.Input;
using BazaarOverlay.WPF.ViewModels;

namespace BazaarOverlay.WPF.Views;

public partial class CardOverlayWindow : Window
{
    private readonly CardOverlayViewModel _viewModel;

    public CardOverlayWindow(CardOverlayViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private async void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(CardOverlayViewModel.IsVisible):
                if (_viewModel.IsVisible)
                {
                    Left = _viewModel.Left;
                    Top = _viewModel.Top;
                    Show();
                }
                else
                {
                    Hide();
                }
                break;

            case nameof(CardOverlayViewModel.CardUrl):
                if (_viewModel.CardUrl is not null)
                {
                    await WebView.EnsureCoreWebView2Async();
                    WebView.CoreWebView2.Navigate(_viewModel.CardUrl);
                }
                break;
        }
    }

    private void DragBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.Hide();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            _viewModel.Hide();
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }

    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        _viewModel.Hide();
    }
}
```

- [ ] **Step 4: Verify solution builds**

Run: `dotnet build src/BazaarOverlay.WPF --no-restore -v minimal`
Expected: Build succeeded.

- [ ] **Step 5: Commit**

```bash
git add src/BazaarOverlay.WPF/Views/CardOverlayWindow.xaml src/BazaarOverlay.WPF/Views/CardOverlayWindow.xaml.cs src/BazaarOverlay.WPF/BazaarOverlay.WPF.csproj
git commit -m "feat: add CardOverlayWindow with WebView2 overlay"
```

---

## Task 10: HotkeyService

**Files:**
- Create: `src/BazaarOverlay.WPF/Services/HotkeyService.cs`

This is a P/Invoke service — tested manually.

- [ ] **Step 1: Write the HotkeyService**

```csharp
// src/BazaarOverlay.WPF/Services/HotkeyService.cs
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace BazaarOverlay.WPF.Services;

public class HotkeyService : IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const int MOD_CONTROL = 0x0002;
    private const int VK_D = 0x44;
    private const int HOTKEY_ID = 9000;

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool UnregisterHotKey(IntPtr hWnd, int id);

    private HwndSource? _hwndSource;
    private IntPtr _windowHandle;

    public event Action? HotkeyPressed;

    public void Register(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;
        _hwndSource = HwndSource.FromHwnd(windowHandle);
        _hwndSource?.AddHook(WndProc);
        RegisterHotKey(windowHandle, HOTKEY_ID, MOD_CONTROL, VK_D);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
        {
            HotkeyPressed?.Invoke();
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        UnregisterHotKey(_windowHandle, HOTKEY_ID);
        _hwndSource?.RemoveHook(WndProc);
        GC.SuppressFinalize(this);
    }
}
```

- [ ] **Step 2: Verify solution builds**

Run: `dotnet build src/BazaarOverlay.WPF --no-restore -v minimal`
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add src/BazaarOverlay.WPF/Services/HotkeyService.cs
git commit -m "feat: add HotkeyService with global Ctrl+D registration"
```

---

## Task 11: OverlayOrchestrator

**Files:**
- Create: `src/BazaarOverlay.Application/Interfaces/IOverlayOrchestrator.cs`
- Create: `src/BazaarOverlay.WPF/Services/OverlayOrchestrator.cs`
- Create: `tests/BazaarOverlay.Tests/WPF/OverlayOrchestratorTests.cs`

- [ ] **Step 1: Write the failing tests**

```csharp
// tests/BazaarOverlay.Tests/WPF/OverlayOrchestratorTests.cs
using BazaarOverlay.Application.Interfaces;
using BazaarOverlay.WPF.Services;
using BazaarOverlay.WPF.ViewModels;
using NSubstitute;
using Shouldly;

namespace BazaarOverlay.Tests.WPF;

public class OverlayOrchestratorTests
{
    private readonly IScreenCaptureService _captureService = Substitute.For<IScreenCaptureService>();
    private readonly IOcrService _ocrService = Substitute.For<IOcrService>();
    private readonly ITooltipNameExtractor _nameExtractor = Substitute.For<ITooltipNameExtractor>();
    private readonly IBazaarDbLookupService _lookupService = Substitute.For<IBazaarDbLookupService>();
    private readonly CardOverlayViewModel _viewModel = new();
    private readonly OverlayOrchestrator _orchestrator;

    public OverlayOrchestratorTests()
    {
        _orchestrator = new OverlayOrchestrator(
            _captureService, _ocrService, _nameExtractor, _lookupService, _viewModel);
    }

    [Fact]
    public async Task HandleHotkeyAsync_IdleState_CapturesAndShowsOverlay()
    {
        _captureService.GetCursorPosition().Returns((500, 300));
        _captureService.CaptureRegion(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(new byte[] { 1, 2, 3 });
        _ocrService.RecognizeTextAsync(Arg.Any<byte[]>())
            .Returns(new List<string> { "Pigomorph", "Tier: Gold" });
        _nameExtractor.ExtractName(Arg.Any<IReadOnlyList<string>>())
            .Returns("Pigomorph");
        _lookupService.GetCardUrlAsync("Pigomorph")
            .Returns("https://bazaardb.gg/items/pigomorph");

        await _orchestrator.HandleHotkeyAsync();

        _viewModel.IsVisible.ShouldBeTrue();
        _viewModel.CardUrl.ShouldBe("https://bazaardb.gg/items/pigomorph");
    }

    [Fact]
    public async Task HandleHotkeyAsync_ShowingState_HidesOverlay()
    {
        // First press — show
        _captureService.GetCursorPosition().Returns((500, 300));
        _captureService.CaptureRegion(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(new byte[] { 1, 2, 3 });
        _ocrService.RecognizeTextAsync(Arg.Any<byte[]>())
            .Returns(new List<string> { "Pigomorph" });
        _nameExtractor.ExtractName(Arg.Any<IReadOnlyList<string>>())
            .Returns("Pigomorph");
        _lookupService.GetCardUrlAsync("Pigomorph")
            .Returns("https://bazaardb.gg/items/pigomorph");

        await _orchestrator.HandleHotkeyAsync();
        _viewModel.IsVisible.ShouldBeTrue();

        // Second press — hide
        await _orchestrator.HandleHotkeyAsync();
        _viewModel.IsVisible.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleHotkeyAsync_OcrReturnsNoName_DoesNotShowOverlay()
    {
        _captureService.GetCursorPosition().Returns((500, 300));
        _captureService.CaptureRegion(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(new byte[] { 1, 2, 3 });
        _ocrService.RecognizeTextAsync(Arg.Any<byte[]>())
            .Returns(new List<string>());
        _nameExtractor.ExtractName(Arg.Any<IReadOnlyList<string>>())
            .Returns((string?)null);

        await _orchestrator.HandleHotkeyAsync();

        _viewModel.IsVisible.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleHotkeyAsync_LookupReturnsNull_DoesNotShowOverlay()
    {
        _captureService.GetCursorPosition().Returns((500, 300));
        _captureService.CaptureRegion(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(new byte[] { 1, 2, 3 });
        _ocrService.RecognizeTextAsync(Arg.Any<byte[]>())
            .Returns(new List<string> { "Unknown" });
        _nameExtractor.ExtractName(Arg.Any<IReadOnlyList<string>>())
            .Returns("Unknown");
        _lookupService.GetCardUrlAsync("Unknown")
            .Returns((string?)null);

        await _orchestrator.HandleHotkeyAsync();

        _viewModel.IsVisible.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleHotkeyAsync_CaptureRegion_CenteredOnCursor()
    {
        _captureService.GetCursorPosition().Returns((500, 400));
        _captureService.CaptureRegion(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(new byte[] { 1, 2, 3 });
        _ocrService.RecognizeTextAsync(Arg.Any<byte[]>())
            .Returns(new List<string>());
        _nameExtractor.ExtractName(Arg.Any<IReadOnlyList<string>>())
            .Returns((string?)null);

        await _orchestrator.HandleHotkeyAsync();

        // 400px wide centered on cursor: x = 500 - 200 = 300
        // 350px above cursor + 100px below: y = 400 - 350 = 50, height = 450
        _captureService.Received(1).CaptureRegion(300, 50, 400, 450);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/BazaarOverlay.Tests --filter "FullyQualifiedName~OverlayOrchestratorTests" --no-restore -v minimal`
Expected: Compilation error — `OverlayOrchestrator` does not exist.

- [ ] **Step 3: Create the interface**

```csharp
// src/BazaarOverlay.Application/Interfaces/IOverlayOrchestrator.cs
namespace BazaarOverlay.Application.Interfaces;

public interface IOverlayOrchestrator
{
    Task HandleHotkeyAsync();
}
```

- [ ] **Step 4: Write the implementation**

```csharp
// src/BazaarOverlay.WPF/Services/OverlayOrchestrator.cs
using BazaarOverlay.Application.Interfaces;
using BazaarOverlay.WPF.ViewModels;

namespace BazaarOverlay.WPF.Services;

public class OverlayOrchestrator : IOverlayOrchestrator
{
    private const int CaptureWidth = 400;
    private const int CaptureAbove = 350;
    private const int CaptureBelow = 100;

    private readonly IScreenCaptureService _captureService;
    private readonly IOcrService _ocrService;
    private readonly ITooltipNameExtractor _nameExtractor;
    private readonly IBazaarDbLookupService _lookupService;
    private readonly CardOverlayViewModel _viewModel;

    public OverlayOrchestrator(
        IScreenCaptureService captureService,
        IOcrService ocrService,
        ITooltipNameExtractor nameExtractor,
        IBazaarDbLookupService lookupService,
        CardOverlayViewModel viewModel)
    {
        _captureService = captureService;
        _ocrService = ocrService;
        _nameExtractor = nameExtractor;
        _lookupService = lookupService;
        _viewModel = viewModel;
    }

    public async Task HandleHotkeyAsync()
    {
        if (_viewModel.IsVisible)
        {
            _viewModel.Hide();
            return;
        }

        var (cursorX, cursorY) = _captureService.GetCursorPosition();

        var captureX = cursorX - CaptureWidth / 2;
        var captureY = cursorY - CaptureAbove;
        var captureHeight = CaptureAbove + CaptureBelow;

        var imageData = _captureService.CaptureRegion(captureX, captureY, CaptureWidth, captureHeight);
        var ocrLines = await _ocrService.RecognizeTextAsync(imageData);
        var entityName = _nameExtractor.ExtractName(ocrLines);

        if (entityName is null)
            return;

        var cardUrl = await _lookupService.GetCardUrlAsync(entityName);
        if (cardUrl is null)
            return;

        _viewModel.ShowCard(cardUrl, cursorX + 20, cursorY - 100);
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test tests/BazaarOverlay.Tests --filter "FullyQualifiedName~OverlayOrchestratorTests" --no-restore -v minimal`
Expected: All 5 tests PASS.

- [ ] **Step 6: Commit**

```bash
git add src/BazaarOverlay.Application/Interfaces/IOverlayOrchestrator.cs src/BazaarOverlay.WPF/Services/OverlayOrchestrator.cs tests/BazaarOverlay.Tests/WPF/OverlayOrchestratorTests.cs
git commit -m "feat: add OverlayOrchestrator tying hotkey to capture/OCR/lookup/display"
```

---

## Task 12: DI Registration & App Wiring

**Files:**
- Modify: `src/BazaarOverlay.WPF/App.xaml.cs` (add new service registrations + hotkey setup)

- [ ] **Step 1: Add new DI registrations to App.xaml.cs ConfigureServices**

Add the following registrations to the `ConfigureServices` method in `App.xaml.cs`, after the existing repository and service registrations:

```csharp
// Add to the Repositories section:
services.AddScoped<ICardUrlCacheRepository, CardUrlCacheRepository>();

// Add to the Application services section:
services.AddScoped<ITooltipNameExtractor, TooltipNameExtractor>();
services.AddScoped<IScreenCaptureService, ScreenCaptureService>();
services.AddScoped<IOcrService, WindowsOcrService>();
services.AddScoped<IBazaarDbLookupService, BazaarDbLookupService>();
services.AddScoped<IOverlayOrchestrator, OverlayOrchestrator>();

// Add Playwright services as singletons (long-lived browser):
services.AddSingleton<IPlaywrightBrowserManager, PlaywrightBrowserManager>();
services.AddSingleton<IPlaywrightSearchService, PlaywrightSearchService>();

// Add overlay components:
services.AddSingleton<CardOverlayViewModel>();
services.AddSingleton<CardOverlayWindow>();
services.AddSingleton<HotkeyService>();
```

Add the required `using` statements:

```csharp
using BazaarOverlay.Infrastructure.Ocr;
using BazaarOverlay.Infrastructure.Playwright;
using BazaarOverlay.Infrastructure.ScreenCapture;
using BazaarOverlay.WPF.Services;
using BazaarOverlay.WPF.Views;
```

- [ ] **Step 2: Wire up hotkey in OnStartup**

Add after `mainWindow.Show();` in `OnStartup`:

```csharp
// Set up global hotkey
var hotkeyService = _serviceProvider.GetRequiredService<HotkeyService>();
var windowHandle = new System.Windows.Interop.WindowInteropHelper(mainWindow).Handle;
hotkeyService.Register(windowHandle);
hotkeyService.HotkeyPressed += async () =>
{
    var orchestrator = _serviceProvider.GetRequiredService<IOverlayOrchestrator>();
    await orchestrator.HandleHotkeyAsync();
};

// Pre-create overlay window (hidden)
_serviceProvider.GetRequiredService<CardOverlayWindow>();

_logger.LogInformation("Overlay hotkey (Ctrl+D) registered");
```

- [ ] **Step 3: Dispose PlaywrightBrowserManager on exit**

Add to `OnExit` before `_serviceProvider?.Dispose()`:

```csharp
if (_serviceProvider?.GetService<IPlaywrightBrowserManager>() is IAsyncDisposable browserManager)
    browserManager.DisposeAsync().AsTask().GetAwaiter().GetResult();
```

- [ ] **Step 4: Verify solution builds**

Run: `dotnet build BazaarOverlay.slnx --no-restore -v minimal`
Expected: Build succeeded.

- [ ] **Step 5: Run all tests**

Run: `dotnet test tests/BazaarOverlay.Tests --no-restore -v minimal`
Expected: All tests PASS (no regressions).

- [ ] **Step 6: Commit**

```bash
git add src/BazaarOverlay.WPF/App.xaml.cs
git commit -m "feat: wire overlay services into DI and register Ctrl+D hotkey"
```

---

## Task 13: Install Playwright Browsers

**Files:** None (runtime setup)

- [ ] **Step 1: Install Playwright browsers**

This is a one-time setup step required before Playwright can work. Run:

```bash
pwsh -Command "dotnet tool install --global Microsoft.Playwright.CLI 2>/dev/null; playwright install chromium"
```

Or via the .NET Playwright installer:

```bash
dotnet build src/BazaarOverlay.Infrastructure
pwsh bin/Debug/net10.0-windows10.0.22621.0/playwright.ps1 install chromium
```

- [ ] **Step 2: Verify the app launches**

Run: `dotnet run --project src/BazaarOverlay.WPF`
Expected: App launches, console shows "Overlay hotkey (Ctrl+D) registered".

- [ ] **Step 3: Commit any changed lock files**

```bash
git add -u
git commit -m "chore: install Playwright Chromium browser"
```

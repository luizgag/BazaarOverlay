# BazaarDB Live Overlay Design

**Date**: 2026-03-26
**Status**: Approved

## Overview

Pivot from local database storage to on-demand access of bazaardb.gg. The user presses Ctrl+D, the app screenshots the area around the mouse cursor, OCRs the tooltip to extract the entity name, looks it up on bazaardb.gg via headless Playwright, and displays the card page in a WebView2 overlay window.

**Guiding principle**: Open/Closed — extend the codebase, do not alter or remove existing code.

## Architecture

Three new subsystems, all additive:

1. **Screen Capture & OCR** — detects what the user is hovering over
2. **BazaarDb Lookup & Caching** — resolves entity names to bazaardb.gg card URLs
3. **Overlay Display** — shows the card page in a floating WebView2 window

## Section 1: Screen Capture & OCR

### Hotkey

- Global hotkey Ctrl+D via `RegisterHotKey` P/Invoke
- Toggles overlay on/off (state machine: Idle ↔ Showing)

### Screen Capture

- On Ctrl+D press, get cursor position via `GetCursorPos` P/Invoke
- Capture rectangle: ~400px wide centered on cursor, ~350px above cursor (upper buffer for item names at top of tooltips), ~100px below cursor
- Capture via `System.Drawing.Graphics.CopyFromScreen`

### OCR

- `Windows.Media.Ocr.OcrEngine` (WinRT API, built into Windows 10+)
- Requires `<TargetPlatformVersion>10.0.22621.0</TargetPlatformVersion>` in WPF .csproj
- Convert captured bitmap to `SoftwareBitmap`, run `OcrEngine.RecognizeAsync`
- Extract text lines, apply heuristics to find entity name (largest/most prominent text, typically first line of tooltip)

### New Interfaces & Implementations

| Interface | Layer | Responsibility |
|---|---|---|
| `IScreenCaptureService` | Application | Captures a bitmap region around cursor position |
| `IOcrService` | Application | Extracts text lines from a bitmap |
| `ITooltipNameExtractor` | Application | Heuristics to pick the entity name from OCR text |
| `ScreenCaptureService` | Infrastructure | P/Invoke implementation |
| `WindowsOcrService` | Infrastructure | WinRT OCR implementation |
| `TooltipNameExtractor` | Infrastructure | Name extraction logic |

## Section 2: BazaarDb Lookup & Caching

### Playwright Search

- On app startup, launch a headless Playwright Chromium instance (singleton, long-lived browser context)
- To look up a name: navigate to `https://bazaardb.gg/search?q={url-encoded-name}`
- Wait for search results to render, extract first result's href (`/card/{id}/{slug}`)
- Build full card URL

### Caching

- SQLite cache reusing existing `BazaarDbContext` pattern
- New entity `CardUrlCache`: Name (string, unique index), CardUrl (string), Category (string), CachedAt (DateTime)
- Lookup flow: cache hit → return URL; cache miss → Playwright search → save to cache → return URL

### New Interfaces & Implementations

| Interface | Layer | Responsibility |
|---|---|---|
| `IBazaarDbLookupService` | Application | `GetCardUrlAsync(string name)` — orchestrates cache + search |
| `ICardUrlCacheRepository` | Domain | `GetByNameAsync`, `SaveAsync` |
| `IPlaywrightSearchService` | Infrastructure | Raw Playwright navigation and scraping |
| `IPlaywrightBrowserManager` | Infrastructure | Singleton browser instance lifecycle |
| `BazaarDbLookupService` | Infrastructure | Implements lookup orchestration |
| `PlaywrightSearchService` | Infrastructure | Manages search page navigation |
| `PlaywrightBrowserManager` | Infrastructure | Creates/disposes browser on app start/stop |
| `CardUrlCacheRepository` | Infrastructure | SQLite repository implementation |
| `CardUrlCache` | Domain | Cache entity |

## Section 3: Overlay Display

### WebView2 Overlay Window

- New WPF `Window` (`CardOverlayWindow`) — borderless, topmost, no taskbar entry
- Contains a `WebView2` control bound to card URL
- Window size: ~450x700px (adjustable later)

### Positioning

- Place window near mouse cursor position on activation
- Clamp to screen bounds to prevent off-screen placement

### Dismissal

- Ctrl+D again → hide overlay
- Escape → hide overlay
- Click outside → hide overlay

### State Machine

```
Idle → [Ctrl+D] → capture/OCR/lookup → Showing
Showing → [Ctrl+D or Escape] → hide → Idle
Showing → [Ctrl+D with new name] → replace card → Showing
```

### New Interfaces & Implementations

| Interface | Layer | Responsibility |
|---|---|---|
| `IOverlayOrchestrator` | Application | Ties hotkey → capture → OCR → lookup → display |
| `CardOverlayViewModel` | WPF | `CardUrl`, `IsVisible`, `ShowCard()`, `Hide()` |
| `CardOverlayWindow` | WPF | Borderless topmost WebView2 window |
| `OverlayOrchestrator` | WPF | Implements orchestration, coordinates with ViewModel |

## Dependencies (New NuGet Packages)

- `Microsoft.Playwright` — headless browser for bazaardb.gg search
- `Microsoft.Web.WebView2` — WebView2 control for overlay display

## Testing Strategy

- **OCR**: Unit tests with mock bitmaps, integration tests with sample tooltip screenshots
- **Playwright search**: Integration tests against live bazaardb.gg (marked as integration, not run in CI)
- **Cache**: Unit tests with in-memory SQLite (existing pattern)
- **Overlay orchestrator**: Unit tests with mocked services
- **Tooltip name extractor**: Unit tests with various OCR output samples

## Future Enhancements

- Auto-detect tooltips without hotkey (continuous screen monitoring)
- Inject CSS/JS to strip bazaardb.gg navigation, ads, and footer for cleaner overlay
- Configurable capture region size
- Configurable hotkey

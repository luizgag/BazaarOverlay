# Flexible OCR Capture Mode Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add configuration-driven toggle between full-screen and rectangle OCR capture modes to enable testing OCR behavior without code changes.

**Architecture:** Create an `IOcrCaptureConfig` service that reads the capture mode from `appsettings.json`. Inject it into `OverlayOrchestrator`, which uses it to decide between two capture strategies: full-screen (capture entire screen) or rectangle (current behavior: 400x450px around cursor). The debug rectangle overlay is removed in both modes.

**Tech Stack:** C# / .NET 10, xUnit, Moq for testing

---

## Task 1: Create IOcrCaptureConfig Interface and Enum

**Files:**
- Create: `src/BazaarOverlay.Application/Interfaces/IOcrCaptureConfig.cs`

- [ ] **Step 1: Create the interface file with enum**

Create `src/BazaarOverlay.Application/Interfaces/IOcrCaptureConfig.cs`:

```csharp
namespace BazaarOverlay.Application.Interfaces;

public interface IOcrCaptureConfig
{
    OcrCaptureModeEnum CaptureMode { get; }
}

public enum OcrCaptureModeEnum
{
    Rectangle,
    FullScreen
}
```

- [ ] **Step 2: Verify file compiles**

Run: `dotnet build src/BazaarOverlay.Application/`

Expected: Build succeeds, no errors

---

## Task 2: Create OcrCaptureConfig Service

**Files:**
- Create: `src/BazaarOverlay.Application/Services/OcrCaptureConfig.cs`
- Test: `tests/BazaarOverlay.Tests/Application/OcrCaptureConfigTests.cs`

- [ ] **Step 1: Write failing test for OcrCaptureConfig**

Create `tests/BazaarOverlay.Tests/Application/OcrCaptureConfigTests.cs`:

```csharp
using BazaarOverlay.Application.Interfaces;
using BazaarOverlay.Application.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BazaarOverlay.Tests.Application;

public class OcrCaptureConfigTests
{
    [Fact]
    public void Constructor_WhenModeIsFullScreen_SetsCaptureModToFullScreen()
    {
        // Arrange
        var configDict = new Dictionary<string, string> { { "OcrCaptureMode", "FullScreen" } };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        // Act
        var ocrConfig = new OcrCaptureConfig(config);

        // Assert
        Assert.Equal(OcrCaptureModeEnum.FullScreen, ocrConfig.CaptureMode);
    }

    [Fact]
    public void Constructor_WhenModeIsRectangle_SetsCaptureModToRectangle()
    {
        // Arrange
        var configDict = new Dictionary<string, string> { { "OcrCaptureMode", "Rectangle" } };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        // Act
        var ocrConfig = new OcrCaptureConfig(config);

        // Assert
        Assert.Equal(OcrCaptureModeEnum.Rectangle, ocrConfig.CaptureMode);
    }

    [Fact]
    public void Constructor_WhenModeNotSet_DefaultsToRectangle()
    {
        // Arrange
        var configDict = new Dictionary<string, string>();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        // Act
        var ocrConfig = new OcrCaptureConfig(config);

        // Assert
        Assert.Equal(OcrCaptureModeEnum.Rectangle, ocrConfig.CaptureMode);
    }

    [Fact]
    public void Constructor_WhenModeIsInvalid_ThrowsInvalidOperationException()
    {
        // Arrange
        var configDict = new Dictionary<string, string> { { "OcrCaptureMode", "InvalidMode" } };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new OcrCaptureConfig(config));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/BazaarOverlay.Tests/Application/OcrCaptureConfigTests.cs -v`

Expected: FAIL - OcrCaptureConfig type does not exist

- [ ] **Step 3: Implement OcrCaptureConfig service**

Create `src/BazaarOverlay.Application/Services/OcrCaptureConfig.cs`:

```csharp
using BazaarOverlay.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace BazaarOverlay.Application.Services;

public class OcrCaptureConfig : IOcrCaptureConfig
{
    public OcrCaptureModeEnum CaptureMode { get; }

    public OcrCaptureConfig(IConfiguration configuration)
    {
        var modeString = configuration["OcrCaptureMode"] ?? "Rectangle";

        try
        {
            CaptureMode = Enum.Parse<OcrCaptureModeEnum>(modeString);
        }
        catch (ArgumentException)
        {
            throw new InvalidOperationException(
                $"Invalid OcrCaptureMode value: '{modeString}'. Valid values are: {string.Join(", ", Enum.GetNames(typeof(OcrCaptureModeEnum)))}");
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/BazaarOverlay.Tests/Application/OcrCaptureConfigTests.cs -v`

Expected: PASS (all 4 tests)

- [ ] **Step 5: Commit**

```bash
git add src/BazaarOverlay.Application/Interfaces/IOcrCaptureConfig.cs \
        src/BazaarOverlay.Application/Services/OcrCaptureConfig.cs \
        tests/BazaarOverlay.Tests/Application/OcrCaptureConfigTests.cs && \
git commit -m "feat: add IOcrCaptureConfig and OcrCaptureConfig service with tests"
```

---

## Task 3: Add Full-Screen Capture Tests to OverlayOrchestrator

**Files:**
- Modify: `tests/BazaarOverlay.Tests/Application/OverlayOrchestratorTests.cs`

- [ ] **Step 1: Add test for full-screen capture mode**

Open `tests/BazaarOverlay.Tests/Application/OverlayOrchestratorTests.cs` and add this test method to the test class:

```csharp
[Fact]
public async Task HandleHotkeyAsync_WhenCapturesModeIsFullScreen_CapturesEntireScreen()
{
    // Arrange
    var mockCaptureService = new Mock<IScreenCaptureService>();
    var mockOcrService = new Mock<IOcrService>();
    var mockNameExtractor = new Mock<ITooltipNameExtractor>();
    var mockLookupService = new Mock<IBazaarDbLookupService>();
    var mockViewModel = new Mock<CardOverlayViewModel>();
    var mockDebugRectWindow = new Mock<IDebugRectWindow>();
    var mockCaptureConfig = new Mock<IOcrCaptureConfig>();

    mockCaptureConfig.Setup(c => c.CaptureMode).Returns(OcrCaptureModeEnum.FullScreen);
    mockCaptureService.Setup(s => s.GetCursorPosition()).Returns((100, 100));
    mockCaptureService.Setup(s => s.CaptureRegion(0, 0, 1920, 1080)).Returns(new byte[] { });
    mockOcrService.Setup(s => s.RecognizeTextAsync(It.IsAny<byte[]>())).ReturnsAsync(new[] { "card", "name" });
    mockNameExtractor.Setup(e => e.ExtractName(It.IsAny<string[]>())).Returns("CardName");
    mockLookupService.Setup(l => l.GetCardUrlAsync("CardName")).ReturnsAsync("https://example.com/card");

    var orchestrator = new OverlayOrchestrator(
        mockCaptureService.Object,
        mockOcrService.Object,
        mockNameExtractor.Object,
        mockLookupService.Object,
        mockViewModel.Object,
        mockDebugRectWindow.Object,
        mockCaptureConfig.Object);

    // Act
    await orchestrator.HandleHotkeyAsync();

    // Assert
    mockCaptureService.Verify(s => s.CaptureRegion(0, 0, 1920, 1080), Times.Once);
    mockViewModel.Verify(v => v.ShowCard("https://example.com/card", 120, 0), Times.Once);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/BazaarOverlay.Tests/Application/OverlayOrchestratorTests.cs::OverlayOrchestratorTests::HandleHotkeyAsync_WhenCapturesModeIsFullScreen_CapturesEntireScreen -v`

Expected: FAIL - OverlayOrchestrator constructor does not accept IOcrCaptureConfig parameter

---

## Task 4: Modify OverlayOrchestrator to Support Configuration

**Files:**
- Modify: `src/BazaarOverlay.Application/Services/OverlayOrchestrator.cs`

- [ ] **Step 1: Add IOcrCaptureConfig to OverlayOrchestrator constructor and fields**

Open `src/BazaarOverlay.Application/Services/OverlayOrchestrator.cs` and update the class:

Replace the constructor and field declarations:

```csharp
private const int CaptureWidth = 400;
private const int CaptureAbove = 350;
private const int CaptureBelow = 100;

private readonly IScreenCaptureService _captureService;
private readonly IOcrService _ocrService;
private readonly ITooltipNameExtractor _nameExtractor;
private readonly IBazaarDbLookupService _lookupService;
private readonly CardOverlayViewModel _viewModel;
private readonly IDebugRectWindow _debugRectWindow;
private readonly IOcrCaptureConfig _captureConfig;

public OverlayOrchestrator(
    IScreenCaptureService captureService,
    IOcrService ocrService,
    ITooltipNameExtractor nameExtractor,
    IBazaarDbLookupService lookupService,
    CardOverlayViewModel viewModel,
    IDebugRectWindow debugRectWindow,
    IOcrCaptureConfig captureConfig)
{
    _captureService = captureService;
    _ocrService = ocrService;
    _nameExtractor = nameExtractor;
    _lookupService = lookupService;
    _viewModel = viewModel;
    _debugRectWindow = debugRectWindow;
    _captureConfig = captureConfig;
}
```

- [ ] **Step 2: Extract rectangle capture logic into helper method**

Add this method to the OverlayOrchestrator class:

```csharp
private (int x, int y, int width, int height) GetRectangleAroundCursor(int cursorX, int cursorY)
{
    var captureX = cursorX - CaptureWidth / 2;
    var captureY = cursorY - CaptureAbove;
    var captureHeight = CaptureAbove + CaptureBelow;

    return (captureX, captureY, CaptureWidth, captureHeight);
}
```

- [ ] **Step 3: Add full-screen capture helper method**

Add this method to the OverlayOrchestrator class:

```csharp
private (int x, int y, int width, int height) GetFullScreenDimensions()
{
    var screenWidth = System.Windows.Forms.Screen.PrimaryScreen?.Bounds.Width ?? 1920;
    var screenHeight = System.Windows.Forms.Screen.PrimaryScreen?.Bounds.Height ?? 1080;

    return (0, 0, screenWidth, screenHeight);
}
```

- [ ] **Step 4: Update HandleHotkeyAsync to use configuration**

Replace the HandleHotkeyAsync method with:

```csharp
public async Task HandleHotkeyAsync()
{
    if (_viewModel.IsVisible)
    {
        _viewModel.Hide();
        _debugRectWindow.Hide();
        return;
    }

    var (cursorX, cursorY) = _captureService.GetCursorPosition();

    var (captureX, captureY, captureWidth, captureHeight) = _captureConfig.CaptureMode switch
    {
        OcrCaptureModeEnum.FullScreen => GetFullScreenDimensions(),
        OcrCaptureModeEnum.Rectangle => GetRectangleAroundCursor(cursorX, cursorY),
        _ => throw new InvalidOperationException($"Unknown capture mode: {_captureConfig.CaptureMode}")
    };

    var imageData = _captureService.CaptureRegion(captureX, captureY, captureWidth, captureHeight);
    var ocrLines = await _ocrService.RecognizeTextAsync(imageData);
    var entityName = _nameExtractor.ExtractName(ocrLines);

    if (entityName is null)
        return;

    var cardUrl = await _lookupService.GetCardUrlAsync(entityName);
    if (cardUrl is null)
        return;

    _viewModel.ShowCard(cardUrl, cursorX + 20, cursorY - 100);
}
```

Note: The `_debugRectWindow.ShowRectangle()` and `_viewModel.ShowDebugRect()` calls are removed.

- [ ] **Step 5: Run the full-screen test again**

Run: `dotnet test tests/BazaarOverlay.Tests/Application/OverlayOrchestratorTests.cs::OverlayOrchestratorTests::HandleHotkeyAsync_WhenCapturesModeIsFullScreen_CapturesEntireScreen -v`

Expected: PASS

- [ ] **Step 6: Run all OverlayOrchestratorTests to check for breakage**

Run: `dotnet test tests/BazaarOverlay.Tests/Application/OverlayOrchestratorTests.cs -v`

Expected: All tests pass (fix any broken tests by updating mocks to include IOcrCaptureConfig parameter)

- [ ] **Step 7: Commit**

```bash
git add src/BazaarOverlay.Application/Services/OverlayOrchestrator.cs \
        tests/BazaarOverlay.Tests/Application/OverlayOrchestratorTests.cs && \
git commit -m "feat: add configuration-based capture mode switching to OverlayOrchestrator"
```

---

## Task 5: Add Rectangle Capture Mode Test

**Files:**
- Modify: `tests/BazaarOverlay.Tests/Application/OverlayOrchestratorTests.cs`

- [ ] **Step 1: Add test for rectangle capture mode**

Add this test method to the OverlayOrchestratorTests class:

```csharp
[Fact]
public async Task HandleHotkeyAsync_WhenCapturesModeIsRectangle_CapturesRegionAroundCursor()
{
    // Arrange
    var mockCaptureService = new Mock<IScreenCaptureService>();
    var mockOcrService = new Mock<IOcrService>();
    var mockNameExtractor = new Mock<ITooltipNameExtractor>();
    var mockLookupService = new Mock<IBazaarDbLookupService>();
    var mockViewModel = new Mock<CardOverlayViewModel>();
    var mockDebugRectWindow = new Mock<IDebugRectWindow>();
    var mockCaptureConfig = new Mock<IOcrCaptureConfig>();

    mockCaptureConfig.Setup(c => c.CaptureMode).Returns(OcrCaptureModeEnum.Rectangle);
    mockCaptureService.Setup(s => s.GetCursorPosition()).Returns((500, 500));
    mockCaptureService.Setup(s => s.CaptureRegion(300, 150, 400, 450)).Returns(new byte[] { });
    mockOcrService.Setup(s => s.RecognizeTextAsync(It.IsAny<byte[]>())).ReturnsAsync(new[] { "card", "name" });
    mockNameExtractor.Setup(e => e.ExtractName(It.IsAny<string[]>())).Returns("CardName");
    mockLookupService.Setup(l => l.GetCardUrlAsync("CardName")).ReturnsAsync("https://example.com/card");

    var orchestrator = new OverlayOrchestrator(
        mockCaptureService.Object,
        mockOcrService.Object,
        mockNameExtractor.Object,
        mockLookupService.Object,
        mockViewModel.Object,
        mockDebugRectWindow.Object,
        mockCaptureConfig.Object);

    // Act
    await orchestrator.HandleHotkeyAsync();

    // Assert
    mockCaptureService.Verify(s => s.CaptureRegion(300, 150, 400, 450), Times.Once);
    mockViewModel.Verify(v => v.ShowCard("https://example.com/card", 520, 400), Times.Once);
}
```

- [ ] **Step 2: Run test to verify it passes**

Run: `dotnet test tests/BazaarOverlay.Tests/Application/OverlayOrchestratorTests.cs::OverlayOrchestratorTests::HandleHotkeyAsync_WhenCapturesModeIsRectangle_CapturesRegionAroundCursor -v`

Expected: PASS

- [ ] **Step 3: Run all OverlayOrchestratorTests**

Run: `dotnet test tests/BazaarOverlay.Tests/Application/OverlayOrchestratorTests.cs -v`

Expected: All tests pass

- [ ] **Step 4: Commit**

```bash
git add tests/BazaarOverlay.Tests/Application/OverlayOrchestratorTests.cs && \
git commit -m "test: add rectangle capture mode test to OverlayOrchestratorTests"
```

---

## Task 6: Register IOcrCaptureConfig in Dependency Injection

**Files:**
- Modify: `src/BazaarOverlay.WPF/App.xaml.cs`

- [ ] **Step 1: Add IOcrCaptureConfig registration to DI container**

Open `src/BazaarOverlay.WPF/App.xaml.cs` and find the service registration code (usually in the constructor or a ConfigureServices method).

Add this line to the service collection before `AddSingleton<IOverlayOrchestrator>()`:

```csharp
services.AddSingleton<IOcrCaptureConfig>(sp =>
    new OcrCaptureConfig(sp.GetRequiredService<IConfiguration>()));
```

Ensure `OcrCaptureConfig` and `IOcrCaptureConfig` are imported:

```csharp
using BazaarOverlay.Application.Interfaces;
using BazaarOverlay.Application.Services;
```

- [ ] **Step 2: Compile and verify no build errors**

Run: `dotnet build src/BazaarOverlay.WPF/`

Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
git add src/BazaarOverlay.WPF/App.xaml.cs && \
git commit -m "feat: register IOcrCaptureConfig in dependency injection container"
```

---

## Task 7: Add OcrCaptureMode Setting to appsettings.json

**Files:**
- Modify: `src/BazaarOverlay.WPF/appsettings.json`

- [ ] **Step 1: Open appsettings.json and add OcrCaptureMode setting**

Open `src/BazaarOverlay.WPF/appsettings.json` and add this line to the root JSON object:

```json
{
  "OcrCaptureMode": "FullScreen"
}
```

If there are existing settings, add it as a sibling key. The file should look something like:

```json
{
  "OcrCaptureMode": "FullScreen",
  "Logging": {
    ...existing settings...
  }
}
```

- [ ] **Step 2: Verify JSON is valid**

Run: `dotnet build src/BazaarOverlay.WPF/`

Expected: Build succeeds (JSON syntax is valid)

- [ ] **Step 3: Commit**

```bash
git add src/BazaarOverlay.WPF/appsettings.json && \
git commit -m "config: set default OcrCaptureMode to FullScreen in appsettings.json"
```

---

## Task 8: Run Full Test Suite

**Files:**
- Test: `tests/BazaarOverlay.Tests/`

- [ ] **Step 1: Run all application tests**

Run: `dotnet test tests/BazaarOverlay.Tests/Application/ -v`

Expected: All tests pass

- [ ] **Step 2: Run full solution tests**

Run: `dotnet test`

Expected: All tests pass, no failures

- [ ] **Step 3: Build the entire solution**

Run: `dotnet build`

Expected: Build succeeds with no warnings or errors

---

## Summary

The implementation adds a flexible OCR capture mode configuration system:

1. **IOcrCaptureConfig** — Interface and enum for capture mode selection
2. **OcrCaptureConfig** — Service that reads mode from `appsettings.json` with full error handling
3. **OverlayOrchestrator** — Updated to use configuration and switch between full-screen and rectangle capture modes
4. **Dependency Injection** — IOcrCaptureConfig registered in DI container
5. **Configuration** — `appsettings.json` updated with default mode (FullScreen)
6. **Tests** — Full test coverage for both capture modes and config service

The debug rectangle overlay is removed from the capture hot path, making testing cleaner. Both capture modes are fully testable and configurable without code changes.

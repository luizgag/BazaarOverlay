# Flexible OCR Capture Mode Design

**Date:** 2026-03-27
**Status:** Approved
**Purpose:** Add configuration-driven toggle between full-screen and rectangle OCR capture modes for testing and debugging

## Context

Currently, the OCR system captures a fixed 400x450px rectangle around the cursor position (350px above, 100px below). OCR results are inconsistent, and we need to test whether the rectangle limitation is causing the issue. This design adds a flexible configuration system to switch between rectangle and full-screen capture modes without code changes.

## Configuration Structure

### IOcrCaptureConfig Interface
```csharp
public interface IOcrCaptureConfig
{
    OcrCaptureModeEnum CaptureMode { get; }
}

public enum OcrCaptureModeEnum
{
    Rectangle,   // Current behavior: 400x450 region around cursor
    FullScreen   // New: capture entire screen
}
```

### Configuration Storage
The mode is read from `appsettings.json`:
```json
{
  "OcrCaptureMode": "FullScreen"
}
```

Default is "Rectangle" if not specified, preserving backward compatibility.

## Implementation

### 1. New Configuration Service
**File:** `src/BazaarOverlay.Application/Services/OcrCaptureConfig.cs`

- Implements `IOcrCaptureConfig`
- Reads `OcrCaptureMode` from `IConfiguration`
- Parses string value to `OcrCaptureModeEnum`
- Throws clear error if invalid mode is configured

### 2. OverlayOrchestrator Changes
**File:** `src/BazaarOverlay.Application/Services/OverlayOrchestrator.cs`

**Constructor:**
- Add `IOcrCaptureConfig _config` parameter

**HandleHotkeyAsync():**
- Remove: `_debugRectWindow.ShowRectangle()` and `_viewModel.ShowDebugRect()` calls (debug rect no longer needed)
- Replace hardcoded rectangle calculation with mode-aware logic

**New private methods:**
- `GetFullScreenDimensions()` → returns (0, 0, screen width, screen height)
- `GetRectangleAroundCursor(cursorX, cursorY)` → returns rectangle calculation (current logic)

**Capture logic:**
```csharp
var (captureX, captureY, captureWidth, captureHeight) = _config.CaptureMode switch
{
    OcrCaptureModeEnum.FullScreen => GetFullScreenDimensions(),
    OcrCaptureModeEnum.Rectangle => GetRectangleAroundCursor(cursorX, cursorY),
    _ => throw new InvalidOperationException($"Unknown capture mode: {_config.CaptureMode}")
};
```

### 3. Dependency Injection
**File:** `src/BazaarOverlay.WPF/App.xaml.cs` (or DI setup location)

Register the configuration service:
```csharp
services.AddSingleton<IOcrCaptureConfig>(sp =>
    new OcrCaptureConfig(sp.GetRequiredService<IConfiguration>()));
```

Ensure `OverlayOrchestrator` receives `IOcrCaptureConfig` in its constructor.

### 4. Remove Debug Rectangle Logic
- Delete or remove the debug rectangle showing calls from `OverlayOrchestrator`
- The `DebugRectWindow` and `IDebugRectWindow` may be left in place (not actively used) or removed entirely if not needed elsewhere

## Testing

### Unit Tests (OverlayOrchestratorTests)
1. **Test full-screen capture mode:** Verify dimensions are (0, 0, screen_width, screen_height)
2. **Test rectangle capture mode:** Verify dimensions calculate correctly around cursor position
3. **Mock IOcrCaptureConfig** for each mode and verify correct helper method is called

### Integration Testing
- Set `OcrCaptureMode` to "FullScreen" in test appsettings
- Run hotkey, verify OCR processes full screen without debug rectangle
- Set to "Rectangle", verify rectangle behavior still works

## Backward Compatibility

- Default mode is "Rectangle" if not specified in appsettings
- Existing tests continue to pass
- Debug rectangle window is not shown in either mode (removed from hot path)

## Success Criteria

- ✅ Configuration reads from appsettings.json without code changes
- ✅ Full-screen capture works and passes OCR text through name extractor
- ✅ Rectangle mode still functions as before
- ✅ No debug rectangle overlay shown (cleaner testing)
- ✅ All existing tests pass
- ✅ New tests cover both capture modes

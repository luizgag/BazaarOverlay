# Menu Toggle Hotkey (CTRL+H) Design

**Date:** 2026-03-27
**Objective:** Add CTRL+H hotkey to toggle MainWindow (menu overlay) visibility, with the window starting hidden on launch.

## Architecture

### HotkeyService Changes

**Current state:** Registers only CTRL+D (Capture hotkey) via hardcoded constants.

**Changes:**
- Add `VK_H = 0x48` constant for the H key
- Add `HOTKEY_ID_MENU = 9001` constant to identify the menu hotkey (separate from Capture's 9000)
- Modify `Register()` to register **both** hotkeys:
  - CTRL+D → HOTKEY_ID = 9000 → raises `HotkeyPressed` event
  - CTRL+H → HOTKEY_ID = 9001 → raises `MenuHotkeyPressed` event (new)
- Update `WndProc()` to differentiate between the two by checking `wParam.ToInt32()` against both IDs
- Update `Dispose()` to unregister both hotkeys

**New event:**
```csharp
public event Action? MenuHotkeyPressed;
```

### MainWindow Changes

**Startup visibility:**
- Set `Visibility="Hidden"` in MainWindow.xaml (or in code-behind constructor)

**Event handling:**
- Subscribe to `HotkeyService.MenuHotkeyPressed` in MainWindow constructor
- On event: toggle `Visibility` between `Hidden` and `Visible`

### App.xaml.cs Wiring

**Current flow:**
- App creates HotkeyService
- App creates MainWindow
- App.xaml.cs passes HotkeyService to OverlayOrchestrator for Ctrl+D handling

**Updated flow:**
- App creates HotkeyService
- App creates MainWindow
- MainWindow subscribes to `HotkeyService.MenuHotkeyPressed` in its constructor
- (Existing OverlayOrchestrator wiring remains unchanged)

## Testing

**Unit tests:** HotkeyService should:
- Register both CTRL+D and CTRL+H successfully
- Fire `HotkeyPressed` when CTRL+D is triggered
- Fire `MenuHotkeyPressed` when CTRL+H is triggered
- Unregister both hotkeys on Dispose

**Integration test:** MainWindow should:
- Start with Visibility = Hidden
- Toggle to Visible when MenuHotkeyPressed fires
- Toggle back to Hidden when MenuHotkeyPressed fires again

## Implementation Order

1. Add constants to HotkeyService
2. Modify Register() to register both hotkeys
3. Update WndProc() to handle both hotkey IDs
4. Add MenuHotkeyPressed event
5. Update Dispose() to unregister both
6. Set MainWindow initial Visibility to Hidden
7. Wire up MenuHotkeyPressed in MainWindow
8. Write and run tests

## Edge Cases

- **Dispose:** Ensure both hotkeys are properly unregistered
- **Window state:** Visibility toggles work even if window is minimized or in taskbar
- **Focus:** Hotkey works regardless of whether the application has focus (global hotkey behavior)

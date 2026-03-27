using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace BazaarOverlay.WPF.Services;

public partial class HotkeyService : IDisposable
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

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

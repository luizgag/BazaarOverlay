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

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

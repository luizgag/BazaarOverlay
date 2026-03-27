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

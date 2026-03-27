using BazaarOverlay.Application.Interfaces;
using BazaarOverlay.Application.Services;
using BazaarOverlay.Application.ViewModels;
using NSubstitute;
using Shouldly;

namespace BazaarOverlay.Tests.Application;

public class OverlayOrchestratorTests
{
    private readonly IScreenCaptureService _captureService = Substitute.For<IScreenCaptureService>();
    private readonly IOcrService _ocrService = Substitute.For<IOcrService>();
    private readonly ITooltipNameExtractor _nameExtractor = Substitute.For<ITooltipNameExtractor>();
    private readonly IBazaarDbLookupService _lookupService = Substitute.For<IBazaarDbLookupService>();
    private readonly CardOverlayViewModel _viewModel = new();
    private readonly IDebugRectWindow _debugRectWindow = Substitute.For<IDebugRectWindow>();
    private readonly IOcrCaptureConfig _captureConfig = Substitute.For<IOcrCaptureConfig>();
    private readonly OverlayOrchestrator _orchestrator;

    public OverlayOrchestratorTests()
    {
        _captureConfig.CaptureMode.Returns(OcrCaptureModeEnum.Rectangle);
        _orchestrator = new OverlayOrchestrator(
            _captureService, _ocrService, _nameExtractor, _lookupService, _viewModel, _debugRectWindow, _captureConfig);
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

    [Fact]
    public async Task HandleHotkeyAsync_WhenCaptureModeIsFullScreen_CapturesEntireScreen()
    {
        // Arrange
        var mockCaptureConfig = Substitute.For<IOcrCaptureConfig>();
        mockCaptureConfig.CaptureMode.Returns(OcrCaptureModeEnum.FullScreen);

        var orchestrator = new OverlayOrchestrator(
            _captureService, _ocrService, _nameExtractor, _lookupService, _viewModel, _debugRectWindow, mockCaptureConfig);

        _captureService.GetCursorPosition().Returns((100, 100));
        _captureService.CaptureRegion(0, 0, 1920, 1080).Returns(new byte[] { });
        _ocrService.RecognizeTextAsync(Arg.Any<byte[]>())
            .Returns(new List<string> { "card", "name" });
        _nameExtractor.ExtractName(Arg.Any<IReadOnlyList<string>>())
            .Returns("CardName");
        _lookupService.GetCardUrlAsync("CardName")
            .Returns("https://example.com/card");

        // Act
        await orchestrator.HandleHotkeyAsync();

        // Assert
        _captureService.Received(1).CaptureRegion(0, 0, 1920, 1080);
        _viewModel.IsVisible.ShouldBeTrue();
        _viewModel.CardUrl.ShouldBe("https://example.com/card");
    }

    [Fact]
    public async Task HandleHotkeyAsync_WhenCaptureModeIsRectangle_CapturesRegionAroundCursor()
    {
        // Arrange
        var mockCaptureConfig = Substitute.For<IOcrCaptureConfig>();
        mockCaptureConfig.CaptureMode.Returns(OcrCaptureModeEnum.Rectangle);

        var orchestrator = new OverlayOrchestrator(
            _captureService, _ocrService, _nameExtractor, _lookupService, _viewModel, _debugRectWindow, mockCaptureConfig);

        _captureService.GetCursorPosition().Returns((500, 500));
        _captureService.CaptureRegion(300, 150, 400, 450).Returns(new byte[] { });
        _ocrService.RecognizeTextAsync(Arg.Any<byte[]>())
            .Returns(new List<string> { "card", "name" });
        _nameExtractor.ExtractName(Arg.Any<IReadOnlyList<string>>())
            .Returns("CardName");
        _lookupService.GetCardUrlAsync("CardName")
            .Returns("https://example.com/card");

        // Act
        await orchestrator.HandleHotkeyAsync();

        // Assert
        _captureService.Received(1).CaptureRegion(300, 150, 400, 450);
        _viewModel.IsVisible.ShouldBeTrue();
        _viewModel.CardUrl.ShouldBe("https://example.com/card");
    }
}

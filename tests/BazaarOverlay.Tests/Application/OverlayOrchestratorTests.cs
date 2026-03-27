using BazaarOverlay.Application.DTOs;
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
            .Returns(new List<OcrTextLine>
            {
                new("Pigomorph", 30, 200, 100),
                new("Tier: Gold", 15, 200, 140),
            });
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
            .Returns(new List<OcrTextLine> { new("Pigomorph", 30, 200, 100) });
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
            .Returns(new List<OcrTextLine>());
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
            .Returns(new List<OcrTextLine> { new("Unknown", 30, 200, 100) });
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
            .Returns(new List<OcrTextLine>());
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
        var mockCaptureConfig = Substitute.For<IOcrCaptureConfig>();
        mockCaptureConfig.CaptureMode.Returns(OcrCaptureModeEnum.FullScreen);

        var orchestrator = new OverlayOrchestrator(
            _captureService, _ocrService, _nameExtractor, _lookupService, _viewModel, _debugRectWindow, mockCaptureConfig);

        _captureService.GetCursorPosition().Returns((100, 100));
        _captureService.CaptureRegion(0, 0, 1920, 1080).Returns(new byte[] { });
        _ocrService.RecognizeTextAsync(Arg.Any<byte[]>())
            .Returns(new List<OcrTextLine> { new("card", 20, 100, 100) });
        _nameExtractor.ExtractName(Arg.Any<IReadOnlyList<string>>())
            .Returns("CardName");
        _lookupService.GetCardUrlAsync("CardName")
            .Returns("https://example.com/card");

        await orchestrator.HandleHotkeyAsync();

        _captureService.Received(1).CaptureRegion(0, 0, 1920, 1080);
        _viewModel.IsVisible.ShouldBeTrue();
        _viewModel.CardUrl.ShouldBe("https://example.com/card");
    }

    [Fact]
    public async Task HandleHotkeyAsync_WhenCaptureModeIsRectangle_CapturesRegionAroundCursor()
    {
        var mockCaptureConfig = Substitute.For<IOcrCaptureConfig>();
        mockCaptureConfig.CaptureMode.Returns(OcrCaptureModeEnum.Rectangle);

        var orchestrator = new OverlayOrchestrator(
            _captureService, _ocrService, _nameExtractor, _lookupService, _viewModel, _debugRectWindow, mockCaptureConfig);

        _captureService.GetCursorPosition().Returns((500, 500));
        _captureService.CaptureRegion(300, 150, 400, 450).Returns(new byte[] { });
        _ocrService.RecognizeTextAsync(Arg.Any<byte[]>())
            .Returns(new List<OcrTextLine> { new("card", 20, 200, 350) });
        _nameExtractor.ExtractName(Arg.Any<IReadOnlyList<string>>())
            .Returns("CardName");
        _lookupService.GetCardUrlAsync("CardName")
            .Returns("https://example.com/card");

        await orchestrator.HandleHotkeyAsync();

        _captureService.Received(1).CaptureRegion(300, 150, 400, 450);
        _viewModel.IsVisible.ShouldBeTrue();
        _viewModel.CardUrl.ShouldBe("https://example.com/card");
    }

    [Fact]
    public async Task HandleHotkeyAsync_FullScreen_FiltersOutDistantOcrLines()
    {
        // Cursor at center of screen (960, 540)
        var mockCaptureConfig = Substitute.For<IOcrCaptureConfig>();
        mockCaptureConfig.CaptureMode.Returns(OcrCaptureModeEnum.FullScreen);

        var orchestrator = new OverlayOrchestrator(
            _captureService, _ocrService, _nameExtractor, _lookupService, _viewModel, _debugRectWindow, mockCaptureConfig);

        _captureService.GetCursorPosition().Returns((960, 540));
        _captureService.CaptureRegion(0, 0, 1920, 1080).Returns(new byte[] { 1 });

        // OCR returns text at various positions:
        // "Welding Helmet" near cursor, "Version: 1.0" far away at bottom-left
        _ocrService.RecognizeTextAsync(Arg.Any<byte[]>())
            .Returns(new List<OcrTextLine>
            {
                new("Welding Helmet", 30, 950, 480),
                new("Version: 1.0", 10, 200, 1070),
            });

        // The name extractor should only receive lines near the cursor
        // "Version: 1.0" at (200, 1070) is far from cursor at (960, 540)
        _nameExtractor.ExtractName(Arg.Is<IReadOnlyList<string>>(lines =>
            lines.Count == 1 && lines[0] == "Welding Helmet"))
            .Returns("Welding Helmet");
        _lookupService.GetCardUrlAsync("Welding Helmet").Returns("https://example.com/wh");

        await orchestrator.HandleHotkeyAsync();

        _viewModel.IsVisible.ShouldBeTrue();
        _viewModel.CardUrl.ShouldBe("https://example.com/wh");
    }

    [Fact]
    public async Task HandleHotkeyAsync_FullScreen_SortsNearbyLinesByHeightDescending()
    {
        var mockCaptureConfig = Substitute.For<IOcrCaptureConfig>();
        mockCaptureConfig.CaptureMode.Returns(OcrCaptureModeEnum.FullScreen);

        var orchestrator = new OverlayOrchestrator(
            _captureService, _ocrService, _nameExtractor, _lookupService, _viewModel, _debugRectWindow, mockCaptureConfig);

        _captureService.GetCursorPosition().Returns((960, 540));
        _captureService.CaptureRegion(0, 0, 1920, 1080).Returns(new byte[] { 1 });

        // Both lines near cursor but different heights
        // "Shield 20" has smaller text, "Frontal Shielding" is the name (larger text)
        _ocrService.RecognizeTextAsync(Arg.Any<byte[]>())
            .Returns(new List<OcrTextLine>
            {
                new("Shield 20", 15, 960, 580),
                new("Frontal Shielding", 28, 960, 540),
            });

        // Name extractor should receive lines sorted by height: Frontal Shielding first
        _nameExtractor.ExtractName(Arg.Is<IReadOnlyList<string>>(lines =>
            lines.Count == 2 && lines[0] == "Frontal Shielding" && lines[1] == "Shield 20"))
            .Returns("Frontal Shielding");
        _lookupService.GetCardUrlAsync("Frontal Shielding").Returns("https://example.com/fs");

        await orchestrator.HandleHotkeyAsync();

        _viewModel.IsVisible.ShouldBeTrue();
    }

    [Fact]
    public async Task HandleHotkeyAsync_FullScreen_LargeDistantTextIgnoredInFavorOfNearbyTooltip()
    {
        // Key scenario: "400" gold text is larger but far from cursor,
        // "Kev's Armory" is the tooltip name near cursor
        var mockCaptureConfig = Substitute.For<IOcrCaptureConfig>();
        mockCaptureConfig.CaptureMode.Returns(OcrCaptureModeEnum.FullScreen);

        var orchestrator = new OverlayOrchestrator(
            _captureService, _ocrService, _nameExtractor, _lookupService, _viewModel, _debugRectWindow, mockCaptureConfig);

        _captureService.GetCursorPosition().Returns((960, 300));
        _captureService.CaptureRegion(0, 0, 1920, 1080).Returns(new byte[] { 1 });

        _ocrService.RecognizeTextAsync(Arg.Any<byte[]>())
            .Returns(new List<OcrTextLine>
            {
                new("400", 40, 960, 900),                          // large but far (gold display)
                new("Kev's Armory", 28, 950, 280),                 // tooltip name near cursor
                new("Sells Health and Shield Items", 15, 950, 320),// tooltip desc near cursor
            });

        // Only nearby lines should reach the extractor; "400" is filtered out
        _nameExtractor.ExtractName(Arg.Is<IReadOnlyList<string>>(lines =>
            lines.Count == 2 &&
            lines[0] == "Kev's Armory" &&
            lines[1] == "Sells Health and Shield Items"))
            .Returns("Kev's Armory");
        _lookupService.GetCardUrlAsync("Kev's Armory").Returns("https://example.com/ka");

        await orchestrator.HandleHotkeyAsync();

        _viewModel.IsVisible.ShouldBeTrue();
        _viewModel.CardUrl.ShouldBe("https://example.com/ka");
    }
}

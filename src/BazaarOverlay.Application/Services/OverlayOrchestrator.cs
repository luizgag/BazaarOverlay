using BazaarOverlay.Application.Interfaces;
using BazaarOverlay.Application.ViewModels;

namespace BazaarOverlay.Application.Services;

public class OverlayOrchestrator : IOverlayOrchestrator
{
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

    private (int x, int y, int width, int height) GetRectangleAroundCursor(int cursorX, int cursorY)
    {
        var captureX = cursorX - CaptureWidth / 2;
        var captureY = cursorY - CaptureAbove;
        var captureHeight = CaptureAbove + CaptureBelow;

        return (captureX, captureY, CaptureWidth, captureHeight);
    }

    private (int x, int y, int width, int height) GetFullScreenDimensions()
    {
        // Return primary screen dimensions (defaults to 1920x1080)
        // In production, this would be obtained from IScreenProvider if more flexibility is needed
        return (0, 0, 1920, 1080);
    }

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
}

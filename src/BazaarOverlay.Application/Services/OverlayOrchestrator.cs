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

    public OverlayOrchestrator(
        IScreenCaptureService captureService,
        IOcrService ocrService,
        ITooltipNameExtractor nameExtractor,
        IBazaarDbLookupService lookupService,
        CardOverlayViewModel viewModel,
        IDebugRectWindow debugRectWindow)
    {
        _captureService = captureService;
        _ocrService = ocrService;
        _nameExtractor = nameExtractor;
        _lookupService = lookupService;
        _viewModel = viewModel;
        _debugRectWindow = debugRectWindow;
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

        var captureX = cursorX - CaptureWidth / 2;
        var captureY = cursorY - CaptureAbove;
        var captureHeight = CaptureAbove + CaptureBelow;

        var imageData = _captureService.CaptureRegion(captureX, captureY, CaptureWidth, captureHeight);
        var ocrLines = await _ocrService.RecognizeTextAsync(imageData);
        var entityName = _nameExtractor.ExtractName(ocrLines);

        if (entityName is null)
            return;

        var cardUrl = await _lookupService.GetCardUrlAsync(entityName);
        if (cardUrl is null)
            return;

        // Show debug rectangle around capture region
        _viewModel.ShowDebugRect(captureX, captureY, CaptureWidth, captureHeight);
        _debugRectWindow.ShowRectangle(captureX, captureY, CaptureWidth, captureHeight);

        _viewModel.ShowCard(cardUrl, cursorX + 20, cursorY - 100);
    }
}

namespace BazaarOverlay.Application.Interfaces;

public interface ITooltipNameExtractor
{
    string? ExtractName(IReadOnlyList<string> ocrLines);
}

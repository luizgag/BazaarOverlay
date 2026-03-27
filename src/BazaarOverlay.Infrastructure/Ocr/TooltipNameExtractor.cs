using BazaarOverlay.Application.Interfaces;

namespace BazaarOverlay.Infrastructure.Ocr;

public class TooltipNameExtractor : ITooltipNameExtractor
{
    public string? ExtractName(IReadOnlyList<string> ocrLines)
    {
        foreach (var line in ocrLines)
        {
            var trimmed = line.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed))
                return trimmed;
        }

        return null;
    }
}

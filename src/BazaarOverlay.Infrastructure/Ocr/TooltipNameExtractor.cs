using System.Text.RegularExpressions;
using BazaarOverlay.Application.Interfaces;

namespace BazaarOverlay.Infrastructure.Ocr;

public partial class TooltipNameExtractor : ITooltipNameExtractor
{
    private static readonly string[] DescriptionPrefixes = ["Sells", "Buy", "Tier"];

    [GeneratedRegex(@"^[+-]?\d")]
    private static partial Regex StatModifierPattern();

    public string? ExtractName(IReadOnlyList<string> ocrLines)
    {
        // First pass: find first non-empty line that looks like a name
        foreach (var line in ocrLines)
        {
            var trimmed = line.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed) && !IsDescriptionLine(trimmed))
                return trimmed;
        }

        // Fallback: return first non-empty line
        foreach (var line in ocrLines)
        {
            var trimmed = line.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed))
                return trimmed;
        }

        return null;
    }

    private static bool IsDescriptionLine(string line) =>
        DescriptionPrefixes.Any(p => line.StartsWith(p, StringComparison.OrdinalIgnoreCase)) ||
        line.Contains(':') ||
        StatModifierPattern().IsMatch(line);
}

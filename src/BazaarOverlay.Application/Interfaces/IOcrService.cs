using BazaarOverlay.Application.DTOs;

namespace BazaarOverlay.Application.Interfaces;

public interface IOcrService
{
    /// <summary>
    /// Extracts text lines from a bitmap image (PNG format byte array).
    /// Returns lines with spatial data (position, height) for proximity filtering.
    /// </summary>
    Task<IReadOnlyList<OcrTextLine>> RecognizeTextAsync(byte[] imageData);
}

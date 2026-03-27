namespace BazaarOverlay.Application.Interfaces;

public interface IOcrService
{
    /// <summary>
    /// Extracts text lines from a bitmap image (PNG format byte array).
    /// </summary>
    Task<IReadOnlyList<string>> RecognizeTextAsync(byte[] imageData);
}

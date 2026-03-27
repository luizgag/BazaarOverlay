using System.Runtime.InteropServices.WindowsRuntime;
using BazaarOverlay.Application.Interfaces;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace BazaarOverlay.Infrastructure.Ocr;

public class WindowsOcrService : IOcrService
{
    public async Task<IReadOnlyList<string>> RecognizeTextAsync(byte[] imageData)
    {
        var ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();
        if (ocrEngine is null)
            return Array.Empty<string>();

        using var stream = new InMemoryRandomAccessStream();
        await stream.WriteAsync(imageData.AsBuffer()).AsTask().ConfigureAwait(false);
        stream.Seek(0);

        var decoder = await BitmapDecoder.CreateAsync(stream).AsTask().ConfigureAwait(false);
        var softwareBitmap = await decoder.GetSoftwareBitmapAsync().AsTask().ConfigureAwait(false);

        var result = await ocrEngine.RecognizeAsync(softwareBitmap).AsTask().ConfigureAwait(false);

        // Sort by average word height descending so larger text (card names) comes first
        return result.Lines
            .OrderByDescending(line => line.Words.Average(w => w.BoundingRect.Height))
            .Select(line => line.Text)
            .ToList();
    }
}

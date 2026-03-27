using System.Runtime.InteropServices.WindowsRuntime;
using BazaarOverlay.Application.DTOs;
using BazaarOverlay.Application.Interfaces;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace BazaarOverlay.Infrastructure.Ocr;

public class WindowsOcrService : IOcrService
{
    public async Task<IReadOnlyList<OcrTextLine>> RecognizeTextAsync(byte[] imageData)
    {
        var ocrEngine = OcrEngine.TryCreateFromUserProfileLanguages();
        if (ocrEngine is null)
            return Array.Empty<OcrTextLine>();

        using var stream = new InMemoryRandomAccessStream();
        await stream.WriteAsync(imageData.AsBuffer()).AsTask().ConfigureAwait(false);
        stream.Seek(0);

        var decoder = await BitmapDecoder.CreateAsync(stream).AsTask().ConfigureAwait(false);
        var softwareBitmap = await decoder.GetSoftwareBitmapAsync().AsTask().ConfigureAwait(false);

        var result = await ocrEngine.RecognizeAsync(softwareBitmap).AsTask().ConfigureAwait(false);

        return result.Lines
            .Select(line =>
            {
                var words = line.Words;
                var avgHeight = words.Average(w => w.BoundingRect.Height);
                var centerX = words.Average(w => w.BoundingRect.X + w.BoundingRect.Width / 2);
                var centerY = words.Average(w => w.BoundingRect.Y + w.BoundingRect.Height / 2);
                return new OcrTextLine(line.Text, avgHeight, centerX, centerY);
            })
            .ToList();
    }
}

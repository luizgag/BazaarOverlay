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
        await stream.WriteAsync(imageData.AsBuffer());
        stream.Seek(0);

        var decoder = await BitmapDecoder.CreateAsync(stream);
        var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

        var result = await ocrEngine.RecognizeAsync(softwareBitmap);

        return result.Lines.Select(line => line.Text).ToList();
    }
}

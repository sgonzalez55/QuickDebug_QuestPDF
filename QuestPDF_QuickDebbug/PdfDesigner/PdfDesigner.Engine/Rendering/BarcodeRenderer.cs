using SkiaSharp;
using ZXing;
using ZXing.SkiaSharp;

namespace PdfQuickDebug.Designer.Rendering;

/// <summary>Genera una imagen PNG de un código de barras/QR vía ZXing.</summary>
public static class BarcodeRenderer
{
    public static byte[]? Render(string? value, string symbology, int height = 44)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        try
        {
            var format = (symbology ?? "CODE_128").ToUpperInvariant() switch
            {
                "QR" or "QRCODE" => BarcodeFormat.QR_CODE,
                "CODE_39" => BarcodeFormat.CODE_39,
                "EAN_13" => BarcodeFormat.EAN_13,
                _ => BarcodeFormat.CODE_128
            };

            var writer = new BarcodeWriter
            {
                Format = format,
                Options = new ZXing.Common.EncodingOptions
                {
                    Height = 200,
                    Width = 600,
                    PureBarcode = format != BarcodeFormat.QR_CODE,
                    Margin = 0
                }
            };

            using var skBitmap = writer.Write(value);
            using var image = SKImage.FromBitmap(skBitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }
        catch
        {
            return null;
        }
    }
}

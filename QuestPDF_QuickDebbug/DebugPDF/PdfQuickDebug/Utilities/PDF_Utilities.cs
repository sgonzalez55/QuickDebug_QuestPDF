using System;
using System.Globalization;
using System.Text;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SkiaSharp;
using ZXing;
using ZXing.SkiaSharp;

public static class PDF_Utils
{
    public static string NumeroALetrasCOP(decimal numero)
    {
        long parteEntera = (long)Math.Floor(numero);
        int centavos = (int)Math.Round((numero - parteEntera) * 100);

        string letras = parteEntera == 0
            ? "CERO"
            : NumeroALetras(parteEntera);

        string moneda = parteEntera == 1 ? "PESO" : "PESOS";

        // if (centavos > 0)
        //     return $"{letras} {moneda} CON {centavos:00}/100 M/CTE";

        return $"{letras} {moneda}";
    }


    private static string NumeroALetras(long numero)
    {
        if (numero < 0)
            return "MENOS " + NumeroALetras(Math.Abs(numero));

        if (numero == 0)
            return "";

        if (numero <= 15)
            return new[]
            {
            "", "UN", "DOS", "TRES", "CUATRO", "CINCO",
            "SEIS", "SIETE", "OCHO", "NUEVE", "DIEZ",
            "ONCE", "DOCE", "TRECE", "CATORCE", "QUINCE"
        }[numero];

        if (numero < 20)
            return "DIEC" + NumeroALetras(numero - 10);

        if (numero == 20)
            return "VEINTE";

        if (numero < 30)
            return "VEINTI" + NumeroALetras(numero - 20);

        if (numero < 100)
        {
            string[] decenas =
            {
            "", "", "VEINTE", "TREINTA", "CUARENTA",
            "CINCUENTA", "SESENTA", "SETENTA",
            "OCHENTA", "NOVENTA"
        };

            return decenas[numero / 10] +
                   ((numero % 10 > 0) ? " Y " + NumeroALetras(numero % 10) : "");
        }

        if (numero == 100)
            return "CIEN";

        if (numero < 200)
            return "CIENTO " + NumeroALetras(numero - 100);

        if (numero < 1000)
        {
            string[] centenas =
            {
            "", "CIENTO", "DOSCIENTOS", "TRESCIENTOS",
            "CUATROCIENTOS", "QUINIENTOS", "SEISCIENTOS",
            "SETECIENTOS", "OCHOCIENTOS", "NOVECIENTOS"
        };

            return centenas[numero / 100] + " " + NumeroALetras(numero % 100);
        }

        if (numero < 2000)
            return "MIL " + NumeroALetras(numero - 1000);

        if (numero < 1000000)
            return NumeroALetras(numero / 1000) + " MIL " + NumeroALetras(numero % 1000);

        if (numero == 1000000)
            return "UN MILLÓN";

        if (numero < 2000000)
            return "UN MILLÓN " + NumeroALetras(numero - 1000000);

        if (numero < 1000000000000)
            return NumeroALetras(numero / 1000000) + " MILLONES " +
                   NumeroALetras(numero % 1000000);

        return "";
    }


}

public static class SkiaSharpHelpers
{
    public static void SkiaSharpCanvas(this IContainer container, Action<SKCanvas, Size> drawOnCanvas)
    {
        container.Svg(size =>
        {
            using var stream = new MemoryStream();
            using (var canvas = SKSvgCanvas.Create(new SKRect(0, 0, size.Width, size.Height), stream))
                drawOnCanvas(canvas, size);

            var svgData = stream.ToArray();
            return Encoding.UTF8.GetString(svgData);
        });
    }
}

public static class DrawBarCodeExtensions
{
    public static void DrawBarCode(ColumnDescriptor container, string code, int height = 80, int width = 300, BarcodeFormat BarCodeFormat = BarcodeFormat.CODE_128)
    {
        // code = code.Replace("(", "").Replace(")", "");
        var barcodeWriter = new BarcodeWriter
        {
            Format = BarCodeFormat,
            Options = new()
            {
                Height = height,
                Width = width,
                PureBarcode = true,
                GS1Format = false,
                Margin = 0
            }
        };
        // Console.WriteLine($"Code: {code} - Height: {height} - Width: {width} ");
        var skBitmap = barcodeWriter.Write(code);

        container.Item().AlignCenter().Height(1f, Unit.Inch).Width(3.5f, Unit.Inch).SkiaSharpCanvas((canvas, size) =>
        {
            float scale = Math.Min(size.Width / skBitmap.Width, size.Height / skBitmap.Height);
            float newWidth = skBitmap.Width * scale;
            float newHeight = skBitmap.Height * scale;
            float x = (size.Width - newWidth) / 2;
            float y = (size.Height - newHeight) / 2;

            SKRect destRect = new SKRect(x, y, x + newWidth, y + newHeight);
            canvas.DrawBitmap(skBitmap, destRect);

            using var paint = new SKPaint
            {
                Color = SKColors.Black
            };

            using var font = new SKFont { Size = 12, Typeface = SKTypeface.Default };
        });
    }
}

public static class StringsProcessing
{
    public static string GenerateEan13(string input12)
    {
        if (input12.Length != 12 || !input12.All(char.IsDigit))
            throw new Exception("EAN-13 requiere 12 dígitos base");

        int sum = 0;
        for (int i = 0; i < 12; i++)
        {
            int digit = input12[i] - '0';
            sum += (i % 2 == 0) ? digit : digit * 3;
        }

        int checksum = (10 - (sum % 10)) % 10;
        return input12 + checksum;
    }
}

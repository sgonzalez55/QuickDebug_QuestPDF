using System.Globalization;
using System.Xml.XPath;
using PdfQuickDebug.Designer.Data;
using PdfQuickDebug.Designer.Model;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace PdfQuickDebug.Designer.Rendering;

/// <summary>Aplica estilos del diseño sobre un IContainer de QuestPDF.</summary>
public static class StyleApplicator
{
    public static IContainer ApplyBox(IContainer container, StyleDesign? style, LayoutDesign? layout)
    {
        if (layout?.Padding != null)
        {
            var p = layout.Padding;
            if (p.Top > 0) container = container.PaddingTop(p.Top);
            if (p.Right > 0) container = container.PaddingRight(p.Right);
            if (p.Bottom > 0) container = container.PaddingBottom(p.Bottom);
            if (p.Left > 0) container = container.PaddingLeft(p.Left);
        }

        if (style?.Background != null)
            container = container.Background(style.Background);

        if (!string.IsNullOrWhiteSpace(style?.BorderColor) && (style.BorderWidth ?? 0.5f) > 0)
            container = container.Border(style.BorderWidth!.Value).BorderColor(style.BorderColor);

        if (style?.Rotation is not null && Math.Abs(style.Rotation.Value) > 0.01f)
            container = container.Rotate(style.Rotation.Value);

        if (style?.VerticalAlign != null)
        {
            container = style.VerticalAlign.ToLowerInvariant() switch
            {
                "middle" => container.AlignMiddle(),
                "bottom" => container.AlignBottom(),
                _ => container.AlignTop()
            };
        }

        if (style?.Align != null)
        {
            container = style.Align.ToLowerInvariant() switch
            {
                "center" => container.AlignCenter(),
                "right" => container.AlignRight(),
                _ => container.AlignLeft()
            };
        }

        return container;
    }

    public static void ApplyText(TextSpanDescriptor text, StyleDesign? style, float defaultSize, string? defaultFontFamily = null)
    {
        text.FontSize(style?.FontSize ?? defaultSize);

        var family = style?.FontFamily ?? defaultFontFamily;
        if (!string.IsNullOrWhiteSpace(family))
            text.FontFamily(family!);

        if (style?.Bold == true) text.Bold();
        if (style?.Italic == true) text.Italic();
        if (style?.Underline == true) text.Underline();
        if (style?.Strikeout == true) text.Strikethrough();
        if (style?.Color != null) text.FontColor(style.Color);
    }
}

/// <summary>Formateo de valores resueltos (moneda, número, fecha, texto).</summary>
public static class Formatting
{
    private static readonly CultureInfo Co = CultureInfo.GetCultureInfo("es-CO");

    public static string Apply(string? value, string? format)
    {
        value ??= string.Empty;

        if (string.IsNullOrWhiteSpace(format) ||
            format.Equals("text", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        var f = format.Trim();

        if (f.StartsWith("date:", StringComparison.OrdinalIgnoreCase))
        {
            var pattern = f[5..];
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                ? date.ToString(pattern, Co)
                : value;
        }

        return f.ToLowerInvariant() switch
        {
            "currency" => ToNumber(value)?.ToString("C0", Co) ?? value,
            "currency2" => ToNumber(value)?.ToString("C2", Co) ?? value,
            "number" => ToNumber(value)?.ToString("N0", Co) ?? value,
            "number2" => ToNumber(value)?.ToString("N2", Co) ?? value,
            "upper" => value.ToUpperInvariant(),
            "lower" => value.ToLowerInvariant(),
            _ => value
        };
    }

    private static decimal? ToNumber(string value)
    {
        var clean = value.Replace("$", string.Empty).Replace(",", string.Empty).Trim();
        return decimal.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out var number)
            ? number
            : null;
    }
}

/// <summary>Evalúa expresiones calculadas simples del diseño.</summary>
public sealed class ExpressionEvaluator
{
    private readonly XmlDataResolver _resolver;

    public ExpressionEvaluator(XmlDataResolver resolver) => _resolver = resolver;

    public string Evaluate(ExpressionDesign? expression, XPathNavigator? current)
    {
        if (expression == null)
            return string.Empty;

        var args = expression.Args.Select(a => EvaluateOperand(a, current)).ToList();

        return expression.Op.ToLowerInvariant() switch
        {
            "concat" => string.Concat(args),
            "add" => Sum(args).ToString(CultureInfo.InvariantCulture),
            "sub" => (ToDecimal(args, 0) - ToDecimal(args, 1)).ToString(CultureInfo.InvariantCulture),
            "mul" => (ToDecimal(args, 0) * ToDecimal(args, 1)).ToString(CultureInfo.InvariantCulture),
            "div" => Divide(args),
            "upper" => (args.FirstOrDefault() ?? string.Empty).ToUpperInvariant(),
            "lower" => (args.FirstOrDefault() ?? string.Empty).ToLowerInvariant(),
            "if" => args.Count >= 2 && !string.IsNullOrEmpty(args[0])
                ? args[1]
                : (args.Count >= 3 ? args[2] : string.Empty),
            _ => string.Join(" ", args)
        };
    }

    private string EvaluateOperand(OperandDesign operand, XPathNavigator? current)
    {
        if (operand.Expression != null)
            return Evaluate(operand.Expression, current);

        if (!string.IsNullOrWhiteSpace(operand.Xpath))
            return _resolver.ResolveString(operand.Xpath, current, operand.Value ?? string.Empty);

        return operand.Value ?? string.Empty;
    }

    private static string Divide(List<string> args)
    {
        var divisor = ToDecimal(args, 1);
        return divisor == 0 ? "0" : (ToDecimal(args, 0) / divisor).ToString(CultureInfo.InvariantCulture);
    }

    private static decimal Sum(List<string> args) => args.Sum(Parse);

    private static decimal ToDecimal(List<string> args, int index)
        => index < args.Count ? Parse(args[index]) : 0m;

    private static decimal Parse(string value)
        => decimal.TryParse(value.Replace(",", string.Empty).Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var number)
            ? number
            : 0m;
}

/// <summary>Carga bytes de una imagen desde data URI, archivo o base64 crudo.</summary>
public static class ImageSource
{
    public static byte[]? Load(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return null;

        try
        {
            if (source.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                var idx = source.IndexOf("base64,", StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                    return Convert.FromBase64String(source[(idx + 7)..]);
            }

            if (File.Exists(source))
                return File.ReadAllBytes(source);

            return Convert.FromBase64String(source);
        }
        catch
        {
            return null;
        }
    }
}

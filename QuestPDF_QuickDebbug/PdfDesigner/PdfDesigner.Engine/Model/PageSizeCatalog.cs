namespace PdfQuickDebug.Designer.Model;

/// <summary>
/// Catálogo central de tamaños de página. Fuente única para el lienzo
/// (ReportDesignSurface), el generador (QuestPDF PageSizes) y el diálogo de
/// configuración de página (PageSetupDialog).
/// </summary>
public static class PageSizeCatalog
{
    public static readonly IReadOnlyDictionary<string, (float W, float H)> Portrait =
        new Dictionary<string, (float, float)>(StringComparer.OrdinalIgnoreCase)
        {
            ["Letter"] = (612f, 792f),
            ["Legal"] = (612f, 1008f),
            ["Tabloid"] = (792f, 1224f),
            ["Executive"] = (522f, 756f),
            ["A3"] = (841.89f, 1190.55f),
            ["A4"] = (595.28f, 841.89f),
            ["A5"] = (419.53f, 595.28f),
        };

    public static readonly IReadOnlyList<string> Sizes =
        new[] { "Letter", "Legal", "Tabloid", "Executive", "A3", "A4", "A5" };

    /// <summary>Obtiene las dimensiones Portrait (pt) de un tamaño, o null si no existe.</summary>
    public static (float W, float H)? TryGet(string size)
        => Portrait.TryGetValue(size ?? string.Empty, out var d) ? d : null;

    /// <summary>Resuelve las dimensiones finales de página (pt) aplicando orientación y custom.</summary>
    public static (float W, float H) Resolve(PageDesign p)
    {
        var (w, h) = TryGet(p.Size ?? "Letter") ?? (612f, 792f);
        if (string.Equals(p.Orientation, "Landscape", StringComparison.OrdinalIgnoreCase))
            (w, h) = (h, w);
        if (p.Width > 0 && p.Height > 0) (w, h) = (p.Width, p.Height);
        return (w, h);
    }

    /// <summary>Etiqueta para el ComboBox del diálogo: "A4  ·  595×842 pt".</summary>
    public static string Label(string size)
    {
        var d = TryGet(size);
        return d is { } dim
            ? $"{size}  ·  {dim.W:0.##} × {dim.H:0.##} pt"
            : size;
    }

    /// <summary>Devuelve el tamaño que mejor coincide con las dimensiones dadas (para prellenar el diálogo) o "Custom".</summary>
    public static string Detect(float width, float height)
    {
        if (width > 0 && height > 0)
            foreach (var (name, d) in Portrait)
                if (Math.Abs(d.W - width) < 0.01f && Math.Abs(d.H - height) < 0.01f)
                    return name;
        return "Custom";
    }
}
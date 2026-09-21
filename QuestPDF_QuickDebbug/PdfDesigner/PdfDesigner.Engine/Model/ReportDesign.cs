namespace PdfQuickDebug.Designer.Model;

/// <summary>
/// Raíz del diseño. Modelo compositivo QuestPDF: tres slots fijos (Header,
/// Content, Footer) cada uno con un árbol de <see cref="LayoutNode"/>.
/// </summary>
public sealed class ReportDesign
{
    public string Version { get; set; } = "3.0";
    public PageDesign Page { get; set; } = new();

    /// <summary>Datos para GetMetadata() y el namespace de export al servicio.</summary>
    public DesignMetadata Metadata { get; set; } = new();

    /// <summary>Prefijos de namespace XML usados por los XPath (ej: cac/cbc/inv).</summary>
    public Dictionary<string, string> Namespaces { get; set; } = new();

    /// <summary>page.Header(): fijo, no pagina.</summary>
    public LayoutNode Header { get; set; } = new() { Type = "column" };

    /// <summary>page.Content(): pagina y fluye.</summary>
    public LayoutNode Content { get; set; } = new() { Type = "column" };

    /// <summary>page.Footer(): fijo, no pagina.</summary>
    public LayoutNode Footer { get; set; } = new() { Type = "column" };

    /// <summary>Expresiones calculadas reutilizables, referenciables por nombre.</summary>
    public Dictionary<string, ExpressionDesign> Expressions { get; set; } = new();
}

public sealed class DesignMetadata
{
    public string Nit { get; set; } = string.Empty;
    public string TemplateName { get; set; } = "designer-template";
    public string ClientName { get; set; } = "Designer";
    public string Version { get; set; } = "1.0";
}

public sealed class PageDesign
{
    public string Size { get; set; } = "Letter";
    public string Orientation { get; set; } = "Portrait";

    /// <summary>Ancho personalizado en puntos. Si &gt; 0 se ignora Size.</summary>
    public float Width { get; set; }

    /// <summary>Alto personalizado en puntos. Si &gt; 0 se ignora Size.</summary>
    public float Height { get; set; }

    public MarginDesign Margins { get; set; } = new();
    public string Unit { get; set; } = "pt";
    public string FontFamily { get; set; } = "Liberation Sans";
    public float DefaultFontSize { get; set; } = 10f;
}

public sealed class MarginDesign
{
    public float Top { get; set; } = 20f;
    public float Right { get; set; } = 20f;
    public float Bottom { get; set; } = 20f;
    public float Left { get; set; } = 20f;
}

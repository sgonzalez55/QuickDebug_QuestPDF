using System.Text.Json.Serialization;

namespace PdfQuickDebug.Designer.Model;

/// <summary>Estilo tipográfico y de caja de un nodo hoja o de una celda.</summary>
public sealed class StyleDesign
{
    public string? FontFamily { get; set; }
    public float? FontSize { get; set; }
    public bool? Bold { get; set; }
    public bool? Italic { get; set; }
    public bool? Underline { get; set; }
    public bool? Strikeout { get; set; }

    /// <summary>left | center | right</summary>
    public string? Align { get; set; }

    /// <summary>top | middle | bottom</summary>
    public string? VerticalAlign { get; set; }

    public string? Color { get; set; }
    public string? Background { get; set; }
    public string? BorderColor { get; set; }
    public float? BorderWidth { get; set; }

    /// <summary>Rotación en grados (sentido horario).</summary>
    public float? Rotation { get; set; }
}

/// <summary>Binding de datos: XPath (preview) o expresión C# sobre InvoiceModel (codegen).</summary>
public sealed class BindingDesign
{
    public string Xpath { get; set; } = string.Empty;

    /// <summary>Expresión C# sobre InvoiceModel/DtoUbl. Vacía → se resuelve por binding-map.json.</summary>
    public string? Expr { get; set; }

    /// <summary>Valor por defecto si el XPath no encuentra nada.</summary>
    public string? Default { get; set; }
}

/// <summary>Padding en puntos (top/right/bottom/left).</summary>
public sealed class PaddingDesign
{
    public float Top { get; set; }
    public float Right { get; set; }
    public float Bottom { get; set; }
    public float Left { get; set; }
}

/// <summary>Envoltura de padding (compatibilidad con StyleApplicator.ApplyBox).</summary>
public sealed class LayoutDesign
{
    public PaddingDesign Padding { get; set; } = new();
}

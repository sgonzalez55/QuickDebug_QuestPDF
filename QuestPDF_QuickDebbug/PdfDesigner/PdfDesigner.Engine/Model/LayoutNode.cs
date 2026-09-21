namespace PdfQuickDebug.Designer.Model;

/// <summary>
/// Nodo del árbol compositivo del reporte. Cada nodo mapea 1:1 a una llamada de
/// la API fluida de QuestPDF (Column/Row/Table/Container/…). No hay coordenadas
/// absolutas: QuestPDF calcula posiciones, wrap y paginación.
///
/// Discriminador JSON: <c>type</c> en { column, row, table, container, spacer,
/// pageBreak, text, field, image, barcode, line }.
/// </summary>
public sealed class LayoutNode
{
    public string Type { get; set; } = "column";
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string? Name { get; set; }
    public bool Visible { get; set; } = true;

    // ── Estructura / contenedor ──────────────────────────────
    public List<LayoutNode> Children { get; set; } = new();
    public PaddingDesign Padding { get; set; } = new();
    public string? Background { get; set; }
    public string? BorderColor { get; set; }
    public float? BorderWidth { get; set; }

    /// <summary>Alto fijo (pt). container/spacer/barcode/imagen.</summary>
    public float? Height { get; set; }

    /// <summary>Ancho constante (pt) cuando el nodo vive en una fila.</summary>
    public float? Width { get; set; }

    /// <summary>Peso relativo cuando el nodo vive en una fila (si Width es null).</summary>
    public float? Weight { get; set; }

    // ── Tabla ────────────────────────────────────────────────
    public List<TableColumn> Columns { get; set; } = new();

    /// <summary>XPath de los registros repetidos (filas). Ej: //inv:Lines/inv:Line.</summary>
    public string? RepeatXPath { get; set; }

    /// <summary>XPath/expresión para agrupar las filas (opcional).</summary>
    public string? GroupBy { get; set; }

    /// <summary>Campos numéricos a sumar por grupo (opcional, codegen).</summary>
    public List<string>? Aggregates { get; set; }

    public bool ShowHeader { get; set; } = true;

    // ── Hojas ────────────────────────────────────────────────
    public string? Text { get; set; }
    public BindingDesign? Binding { get; set; }
    public string? Format { get; set; }

    /// <summary>Imagen: ruta de archivo, data URI o base64.</summary>
    public string? Source { get; set; }

    /// <summary>Imagen: contain | cover | fill.</summary>
    public string? Fit { get; set; } = "contain";

    /// <summary>Barcode: CODE_128 | CODE_39 | EAN_13 | QR.</summary>
    public string? Symbology { get; set; }

    /// <summary>Línea: horizontal | vertical.</summary>
    public string? Direction { get; set; } = "horizontal";

    public float Thickness { get; set; } = 1f;

    /// <summary>Alto del spacer cuando Type=spacer.</summary>
    public float SpacerHeight { get; set; } = 12f;

    public StyleDesign? Style { get; set; }
    public ExpressionDesign? Expression { get; set; }
}

/// <summary>Columna de una tabla.</summary>
public sealed class TableColumn
{
    public string Header { get; set; } = string.Empty;
    public BindingDesign? Binding { get; set; }
    public string? Format { get; set; }
    public float? Width { get; set; }
    public float? Relative { get; set; }
    public StyleDesign? Style { get; set; }
}

using PdfQuickDebug.Designer.Model;

namespace PdfQuickDebug.Designer.WinForms;

/// <summary>Crea nodos compositivos con valores por defecto.</summary>
public static class NodeFactory
{
    public static LayoutNode Create(string type) => type switch
    {
        "column" => new LayoutNode { Type = "column" },
        "row" => new LayoutNode
        {
            Type = "row",
            Children =
            {
                new LayoutNode { Type = "text", Text = "A", Weight = 1 },
                new LayoutNode { Type = "text", Text = "B", Weight = 1 }
            }
        },
        "container" => new LayoutNode
        {
            Type = "container",
            Padding = new PaddingDesign { Top = 8, Right = 8, Bottom = 8, Left = 8 },
            Children = { new LayoutNode { Type = "text", Text = "Contenido" } }
        },
        "table" => new LayoutNode
        {
            Type = "table",
            Columns =
            {
                new TableColumn { Header = "Campo", Binding = new BindingDesign { Xpath = "" }, Relative = 1 },
                new TableColumn { Header = "Valor", Binding = new BindingDesign { Xpath = "" }, Relative = 1 }
            }
        },
        "spacer" => new LayoutNode { Type = "spacer", SpacerHeight = 12f },
        "pageBreak" => new LayoutNode { Type = "pageBreak" },
        "text" => new LayoutNode { Type = "text", Text = "Texto", Style = new StyleDesign { FontSize = 10, Color = "#000000" } },
        "field" => new LayoutNode
        {
            Type = "field",
            Binding = new BindingDesign { Xpath = "", Default = "" },
            Format = "text",
            Style = new StyleDesign { FontSize = 10, Color = "#000000" }
        },
        "image" => new LayoutNode { Type = "image", Source = "", Height = 60, Width = 100 },
        "barcode" => new LayoutNode
        {
            Type = "barcode",
            Binding = new BindingDesign { Xpath = "", Default = "" },
            Symbology = "QR",
            Height = 80,
            Width = 80
        },
        "line" => new LayoutNode { Type = "line", Direction = "horizontal", Thickness = 1, Style = new StyleDesign { Color = "#000000" } },
        _ => new LayoutNode { Type = "text", Text = type }
    };

    public static (string Type, string Label)[] Palette() => new[]
    {
        ("column", "Columna"),
        ("row", "Fila"),
        ("container", "Contenedor"),
        ("table", "Tabla"),
        ("spacer", "Espacio"),
        ("text", "Texto"),
        ("field", "Campo"),
        ("image", "Imagen"),
        ("barcode", "Código"),
        ("line", "Línea"),
        ("pageBreak", "Salto")
    };
}

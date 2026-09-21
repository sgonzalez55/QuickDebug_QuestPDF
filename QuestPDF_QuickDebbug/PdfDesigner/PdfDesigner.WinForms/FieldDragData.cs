namespace PdfQuickDebug.Designer.WinForms;

/// <summary>Payload al arrastrar un campo del XML desde el panel de datos.</summary>
public sealed class FieldDragData
{
    public string Name { get; set; } = string.Empty;
    public string XPath { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string? Expr { get; set; }
    public string? Default { get; set; }
    public string? SampleValue { get; set; }
    public bool IsRepeated { get; set; }

    /// <summary>Campos hijos (para crear tablas/detail al soltar un nodo repetido).</summary>
    public List<FieldDragData> Children { get; set; } = new();
}

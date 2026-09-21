using PdfQuickDebug.Designer.Model;

namespace PdfQuickDebug.Designer.WinForms;

/// <summary>Payload al arrastrar un elemento visual desde la paleta (tipo de nodo a crear).</summary>
public sealed class NodeDragData
{
    public string Type { get; set; } = "text";
}

/// <summary>Payload al arrastrar un nodo existente dentro del árbol (para moverlo).</summary>
public sealed class NodeMoveData
{
    public LayoutNode Node { get; set; } = null!;
}

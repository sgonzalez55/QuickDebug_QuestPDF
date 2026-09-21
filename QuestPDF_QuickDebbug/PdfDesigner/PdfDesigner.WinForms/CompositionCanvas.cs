using System.Drawing.Drawing2D;
using PdfQuickDebug.Designer.Data;
using PdfQuickDebug.Designer.Model;

namespace PdfQuickDebug.Designer.WinForms;

/// <summary>
/// Lienzo WYSIWYG del diseño compositivo: dibuja el árbol de nodos
/// (Column/Row/Container/Table/hojas) como un esquema del layout real.
/// Clic selecciona un nodo; arrastrar mueve; soltar inserta campos/elementos.
/// </summary>
public sealed class CompositionCanvas : Control
{
    private const float Margin = 16f;
    private const float Gap = 5f;
    private const float BandLabelH = 24f;

    private ReportDesign _design = new();
    private XmlDataResolver? _resolver;
    private LayoutNode? _selected;
    private readonly List<(LayoutNode Node, RectangleF Rect, int Depth)> _boxes = new();

    private Point _downPoint;
    private LayoutNode? _downNode;

    public event EventHandler? SelectionChanged;

    public CompositionCanvas()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint
                 | ControlStyles.UserPaint
                 | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.ResizeRedraw
                 | ControlStyles.Selectable, true);
        BackColor = Color.FromArgb(232, 235, 240);
        Font = new Font("Segoe UI", 8.5f);
    }

    public ReportDesign Design
    {
        get => _design;
        set { _design = value ?? new ReportDesign(); Rebuild(); }
    }

    public XmlDataResolver? Resolver
    {
        get => _resolver;
        set { _resolver = value; Rebuild(); }
    }

    public LayoutNode? SelectedNode => _selected;

    public void Select(LayoutNode? node)
    {
        _selected = node;
        Invalidate();
    }

    public void Rebuild()
    {
        _boxes.Clear();

        var (pageW, _) = PageSizeCatalog.Resolve(_design.Page);
        var m = _design.Page.Margins ?? new MarginDesign();
        var contentW = Math.Max(120f, pageW - m.Left - m.Right);

        float y = Margin;
        y += Measure(_design.Header, Margin, y, contentW, 0) + Gap;
        y += Measure(_design.Content, Margin, y, contentW, 0) + Gap;
        y += Measure(_design.Footer, Margin, y, contentW, 0);

        Size = new Size((int)(pageW + Margin * 2), (int)(y + Margin + 20));
        Invalidate();
    }

    public LayoutNode? NodeAt(Point client)
    {
        (LayoutNode Node, RectangleF Rect, int Depth)? best = null;
        foreach (var box in _boxes)
        {
            if (box.Rect.Contains(client) && (best is null || box.Depth >= best.Value.Depth))
                best = box;
        }
        return best?.Node;
    }

    /* ── Medición ──────────────────────────────────────────── */

    private float Measure(LayoutNode node, float x, float y, float width, int depth)
    {
        if (!node.Visible) return 0;

        float h = node.Type switch
        {
            "column" => MeasureColumn(node, x, y, width, depth),
            "row" => MeasureRow(node, x, y, width, depth),
            "container" => MeasureContainer(node, x, y, width, depth),
            "table" => MeasureTable(node, x, y, width, depth),
            _ => MeasureLeaf(node, x, y, width, depth)
        };

        return h;
    }

    private bool IsSlotRoot(LayoutNode node)
        => ReferenceEquals(node, _design.Header)
           || ReferenceEquals(node, _design.Content)
           || ReferenceEquals(node, _design.Footer);

    private float MeasureColumn(LayoutNode node, float x, float y, float width, int depth)
    {
        var labelH = IsSlotRoot(node) ? BandLabelH : 0f;
        var cy = y + labelH;
        foreach (var child in node.Children)
            cy += Measure(child, x, cy, width, depth + 1) + Gap;
        var h = Math.Max(labelH + (node.Children.Count == 0 ? 28f : 0f), cy - y);
        _boxes.Add((node, new RectangleF(x, y, width, h), depth));
        return h;
    }

    private float MeasureRow(LayoutNode node, float x, float y, float width, int depth)
    {
        var totalWeight = node.Children.Where(c => c.Width is null or <= 0).Sum(c => c.Weight ?? 1f);
        if (totalWeight <= 0) totalWeight = 1f;

        var cx = x;
        var maxH = 0f;
        foreach (var child in node.Children)
        {
            var cw = child.Width is > 0
                ? child.Width.Value
                : width * (child.Weight ?? 1f) / totalWeight;
            var h = Measure(child, cx, y, cw, depth + 1);
            cx += cw;
            maxH = Math.Max(maxH, h);
        }

        _boxes.Add((node, new RectangleF(x, y, width, maxH), depth));
        return maxH;
    }

    private float MeasureContainer(LayoutNode node, float x, float y, float width, int depth)
    {
        var p = node.Padding;
        var innerW = Math.Max(10f, width - p.Left - p.Right);
        var cy = y + p.Top;
        foreach (var child in node.Children)
            cy += Measure(child, x + p.Left, cy, innerW, depth + 1) + Gap;
        var h = Math.Max(20f, cy - y + p.Bottom);
        _boxes.Add((node, new RectangleF(x, y, width, h), depth));
        return h;
    }

    private float MeasureTable(LayoutNode node, float x, float y, float width, int depth)
    {
        var headerH = 20f;
        var rows = ResolveRowCount(node);
        var h = headerH + rows * 18f;
        _boxes.Add((node, new RectangleF(x, y, width, h), depth));
        return h;
    }

    private int ResolveRowCount(LayoutNode table)
    {
        if (_resolver == null || string.IsNullOrWhiteSpace(table.RepeatXPath)) return 3;
        try
        {
            var count = _resolver.ResolveNodes(table.RepeatXPath).Count;
            return Math.Clamp(count == 0 ? 3 : count, 1, 6);
        }
        catch
        {
            return 3;
        }
    }

    private float MeasureLeaf(LayoutNode node, float x, float y, float width, int depth)
    {
        var font = node.Style?.FontSize ?? _design.Page.DefaultFontSize;
        var lineH = Math.Max(font * 1.4f, 16f);

        float h = node.Type switch
        {
            "spacer" => Math.Max(4f, node.Height ?? node.SpacerHeight),
            "pageBreak" => 8f,
            "line" => Math.Max(6f, node.Thickness + 4f),
            "image" => node.Height ?? 60f,
            "barcode" => node.Height ?? 80f,
            _ => Math.Max(lineH, node.Height ?? lineH)
        };

        var w = node.Width is > 0 ? Math.Min(node.Width.Value, width) : width;

        _boxes.Add((node, new RectangleF(x, y, w, h), depth));
        return h;
    }

    /* ── Pintura ───────────────────────────────────────────── */

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        foreach (var (node, rect, depth) in _boxes)
            DrawNode(g, node, rect);
    }

    private void DrawNode(Graphics g, LayoutNode node, RectangleF rect)
    {
        var selected = ReferenceEquals(_selected, node);
        var fill = NodeColor(node);
        var isBand = IsSlotRoot(node);

        if (node.Type == "pageBreak")
        {
            using var pen = new Pen(Color.FromArgb(160, 170, 185), 1f) { DashStyle = DashStyle.Dash };
            g.DrawLine(pen, rect.Left, rect.Top + rect.Height / 2, rect.Right, rect.Top + rect.Height / 2);
            DrawText(g, "Salto de página", rect, Color.FromArgb(120, 130, 145), alignRight: true);
            return;
        }

        using (var back = new SolidBrush(fill))
            g.FillRectangle(back, rect);

        using (var pen = new Pen(selected ? Color.FromArgb(0, 150, 136) : Color.FromArgb(170, 178, 190),
                   selected ? 2f : 1f))
        {
            if (!selected && node.Type == "spacer")
                pen.DashStyle = DashStyle.Dot;
            g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
        }

        if (node.Type == "table")
            DrawTable(g, node, rect);
        else if (node.Type == "row")
            DrawRowDividers(g, node, rect);

        var label = isBand ? BandLabel(node) : NodeLabel(node);

        if (isBand)
        {
            var stripH = Math.Min(BandLabelH, rect.Height);
            using (var strip = new SolidBrush(Color.FromArgb(222, 227, 236)))
                g.FillRectangle(strip, rect.X, rect.Y, rect.Width, stripH);
            DrawText(g, label, new RectangleF(rect.X + 4, rect.Y, rect.Width - 8, stripH), Color.FromArgb(50, 60, 75), bold: true);
        }
        else
        {
            DrawText(g, label, rect, Color.FromArgb(60, 70, 85));
        }
    }

    private string BandLabel(LayoutNode node)
    {
        if (ReferenceEquals(node, _design.Header)) return "HEADER  (cabecera de página)";
        if (ReferenceEquals(node, _design.Footer)) return "FOOTER  (pie de página)";
        return "CONTENIDO  (fluye y pagina)";
    }

    private void DrawTable(Graphics g, LayoutNode node, RectangleF rect)
    {
        var cols = ColumnWidths(node, rect.Width);
        float x = rect.X;
        using var pen = new Pen(Color.FromArgb(200, 206, 214), 0.6f);

        for (var i = 0; i < cols.Count; i++)
        {
            var cw = cols[i];
            if (i < cols.Count - 1)
                g.DrawLine(pen, x + cw, rect.Top, x + cw, rect.Bottom);
            // header
            var header = node.Columns[i].Header ?? string.Empty;
            DrawText(g, header, new RectangleF(x + 3, rect.Top + 1, cw - 4, 18), Color.FromArgb(0, 90, 150), bold: true);
            x += cw;
        }
        g.DrawLine(pen, rect.Left, rect.Top + 19, rect.Right, rect.Top + 19);
    }

    private void DrawRowDividers(Graphics g, LayoutNode node, RectangleF rect)
    {
        var totalWeight = node.Children.Where(c => c.Width is null or <= 0).Sum(c => c.Weight ?? 1f);
        if (totalWeight <= 0) totalWeight = 1f;
        float x = rect.X;
        using var pen = new Pen(Color.FromArgb(210, 215, 224), 0.6f);
        for (var i = 0; i < node.Children.Count - 1; i++)
        {
            var child = node.Children[i];
            var cw = child.Width is > 0 ? child.Width.Value : rect.Width * (child.Weight ?? 1f) / totalWeight;
            x += cw;
            g.DrawLine(pen, x, rect.Top, x, rect.Bottom);
        }
    }

    private static List<float> ColumnWidths(LayoutNode table, float width)
    {
        var result = new List<float>();
        if (table.Columns.Count == 0) return result;
        var totalRelative = table.Columns.Where(c => c.Width is null or <= 0).Sum(c => c.Relative ?? 1f);
        if (totalRelative <= 0) totalRelative = 1f;
        foreach (var col in table.Columns)
            result.Add(col.Width is > 0 ? col.Width.Value : width * (col.Relative ?? 1f) / totalRelative);
        return result;
    }

    private static Color NodeColor(LayoutNode node) => node.Type switch
    {
        "column" or "container" => Color.FromArgb(250, 251, 253),
        "row" => Color.FromArgb(240, 247, 250),
        "table" => Color.FromArgb(246, 251, 246),
        "field" => Color.FromArgb(255, 249, 226),
        "text" => Color.White,
        "image" => Color.FromArgb(243, 238, 252),
        "barcode" => Color.FromArgb(233, 247, 250),
        "line" => Color.FromArgb(245, 245, 245),
        "spacer" => Color.FromArgb(238, 240, 243),
        _ => Color.White
    };

    private static string NodeLabel(LayoutNode node)
    {
        return node.Type switch
        {
            "column" => "Columna",
            "row" => "Fila",
            "container" => "Contenedor",
            "table" => "Tabla" + (string.IsNullOrWhiteSpace(node.RepeatXPath) ? "" : " · " + Short(node.RepeatXPath, 22)),
            "spacer" => "Espacio",
            "pageBreak" => "Salto",
            "text" => "Texto: " + Short(node.Text, 24),
            "field" => "Campo: " + Short(node.Binding?.Xpath ?? node.Binding?.Expr ?? "—", 24),
            "image" => "Imagen",
            "barcode" => "Código: " + (node.Symbology ?? "—"),
            "line" => "Línea",
            _ => node.Type
        };
    }

    private void DrawText(Graphics g, string text, RectangleF rect, Color color, bool bold = false, bool alignRight = false)
    {
        using var font = new Font(Font, bold ? FontStyle.Bold : FontStyle.Regular);
        using var brush = new SolidBrush(color);
        var sf = new StringFormat
        {
            Alignment = alignRight ? StringAlignment.Far : StringAlignment.Near,
            LineAlignment = StringAlignment.Center,
            Trimming = StringTrimming.EllipsisCharacter,
            FormatFlags = StringFormatFlags.NoWrap
        };
        var r = alignRight ? new RectangleF(rect.X, rect.Y, rect.Width - 4, Math.Min(rect.Height, 16f)) : rect;
        g.DrawString(text, font, brush, r, sf);
    }

    private static string Short(string? value, int max)
        => string.IsNullOrEmpty(value) ? string.Empty : (value.Length > max ? value[..max] + "…" : value);

    /* ── Mouse: selección y arrastre para mover ────────────── */

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;
        Focus();
        _downPoint = e.Location;
        _downNode = NodeAt(e.Location);
        _selected = _downNode;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (e.Button != MouseButtons.Left || _downNode == null) return;
        if (Math.Abs(e.X - _downPoint.X) < SystemInformation.DragSize.Width &&
            Math.Abs(e.Y - _downPoint.Y) < SystemInformation.DragSize.Height)
            return;

        var node = _downNode;
        _downNode = null;
        if (!IsSlotRoot(node))
            DoDragDrop(new NodeMoveData { Node = node }, DragDropEffects.Move);
    }
}

using System.Drawing.Drawing2D;

namespace PdfQuickDebug.Designer.WinForms;

public enum ExplorerStatus
{
    Ok,
    NoData,
    NoMapping
}

/// <summary>Un campo disponible en el explorador de datos (contrato o XML).</summary>
public sealed class ExplorerField
{
    public string Group { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string XPath { get; set; } = string.Empty;
    public string? RelativePath { get; set; }
    public string? Expr { get; set; }
    public string? SampleValue { get; set; }
    public bool IsRepeated { get; set; }
    public ExplorerStatus Status { get; set; } = ExplorerStatus.NoMapping;
    public FieldDragData DragData { get; set; } = new();
}

/// <summary>
/// Explorador de campos (estilo Field Explorer): lista plana, agrupada y con
/// buscador. Arrastrar una fila inicia un drag con imagen de avance junto al
/// cursor y suelta un <see cref="FieldDragData"/> sobre el lienzo.
/// </summary>
public sealed class FieldExplorerPanel : UserControl
{
    private readonly TextBox _search = new();
    private readonly FieldList _list = new();
    private IReadOnlyList<ExplorerField> _all = Array.Empty<ExplorerField>();

    public FieldExplorerPanel()
    {
        BackColor = Color.White;
        Dock = DockStyle.Fill;

        _search = new TextBox
        {
            Dock = DockStyle.Top,
            PlaceholderText = "🔍  Buscar campo o valor...",
            Font = new Font("Segoe UI", 9f),
            Margin = new Padding(0)
        };

        _list.Dock = DockStyle.Fill;

        Controls.Add(_list);
        Controls.Add(_search);
        _search.TextChanged += (_, _) => RefreshFilter();
    }

    public void SetFields(IReadOnlyList<ExplorerField> fields)
    {
        _all = fields ?? Array.Empty<ExplorerField>();
        RefreshFilter();
    }

    private void RefreshFilter()
    {
        var query = _search.Text.Trim();
        IEnumerable<ExplorerField> items = _all;

        if (query.Length > 0)
        {
            items = items.Where(f =>
                f.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                (f.SampleValue ?? string.Empty).Contains(query, StringComparison.OrdinalIgnoreCase) ||
                (f.XPath ?? string.Empty).Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        _list.SetItems(items.ToList(), query);
    }
}

internal sealed class FieldList : ScrollableControl
{
    private const int RowHeight = 22;
    private const int HeaderHeight = 20;

    private List<ExplorerField> _items = new();
    private string _query = string.Empty;

    private int _pendingIndex = -1;
    private Point _downPoint;

    public FieldList()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint
                 | ControlStyles.UserPaint
                 | ControlStyles.OptimizedDoubleBuffer
                 | ControlStyles.ResizeRedraw, true);
        BackColor = Color.White;
        Font = new Font("Segoe UI", 8.5f);
        AutoScroll = true;
    }

    private int ContentHeight
        => _items.Count == 0
            ? 0
            : _items.Count * RowHeight + _items.Select(f => f.Group).Distinct().Count() * HeaderHeight;

    public void SetItems(List<ExplorerField> items, string query)
    {
        _items = items ?? new List<ExplorerField>();
        _query = query ?? string.Empty;
        AutoScrollMinSize = new Size(0, ContentHeight);
        Invalidate();
    }

    /* ── Geometría ─────────────────────────────────────────── */
    private List<(ExplorerField Field, Rectangle Row)> Layout()
    {
        var rows = new List<(ExplorerField, Rectangle)>();
        var y = 0;
        string? lastGroup = null;

        foreach (var item in _items)
        {
            if (item.Group != lastGroup)
            {
                lastGroup = item.Group;
                y += HeaderHeight;
            }

            rows.Add((item, new Rectangle(0, y, Width, RowHeight)));
            y += RowHeight;
        }

        return rows;
    }

    private int IndexAt(int y)
    {
        var rows = Layout();
        for (var i = 0; i < rows.Count; i++)
        {
            if (rows[i].Row.Contains(1, y))
                return i;
        }
        return -1;
    }

    /* ── Pintura ───────────────────────────────────────────── */
    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        g.TranslateTransform(AutoScrollPosition.X, AutoScrollPosition.Y);

        if (_items.Count == 0)
        {
            if (!string.IsNullOrWhiteSpace(_query))
                DrawCentered(g, "Sin resultados para:\n\"" + _query + "\"", Color.Gray);
            else
                DrawCentered(g, "Sin campos.\nUsa  🔎 Campos  para descubrir el contrato.", Color.Gray);
            return;
        }

        var groupCounts = _items.GroupBy(f => f.Group)
            .ToDictionary(gd => gd.Key, gd => gd.Count());

        var rows = Layout();
        string? lastGroup = null;
        var rowIndex = 0;

        foreach (var item in _items)
        {
            if (item.Group != lastGroup)
            {
                lastGroup = item.Group;
                DrawGroupHeader(g, rows[rowIndex].Row.Y - HeaderHeight, lastGroup,
                    groupCounts.GetValueOrDefault(lastGroup));
            }

            DrawRow(g, rows[rowIndex].Row, item);
            rowIndex++;
        }
    }

    private static void DrawCentered(Graphics g, string text, Color color)
    {
        using var brush = new SolidBrush(color);
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(text, SystemFonts.DefaultFont, brush, new RectangleF(0, 0, 230, 300), sf);
    }

    private void DrawGroupHeader(Graphics g, int y, string group, int count)
    {
        using var back = new SolidBrush(Color.FromArgb(233, 237, 242));
        g.FillRectangle(back, 0, y, Width, HeaderHeight);
        using var brush = new SolidBrush(Color.FromArgb(60, 70, 85));
        g.DrawString($"  {group.ToUpperInvariant()}  ({count})", GroupFont, brush, 0, y + 4);
    }

    private void DrawRow(Graphics g, Rectangle rect, ExplorerField item)
    {
        var cy = rect.Y + RowHeight / 2f;

        // Indicador de estado
        using (var fill = new SolidBrush(StatusColor(item.Status)))
        {
            g.FillEllipse(fill, 7, cy - 5, 10, 10);
        }
        DrawStatusGlyph(g, item.Status, cy - 5);

        // Nombre
        var nameX = 24f;
        var available = Width - nameX - 6;
        var nameFont = item.IsRepeated ? NameFontBold : NameFontRegular;
        using var nameBrush = new SolidBrush(item.IsRepeated ? Color.FromArgb(0, 90, 150) : Color.FromArgb(31, 35, 40));
        var name = item.IsRepeated ? item.Name + "   [rep]" : item.Name;

        string nameText, valueText;
        MeasureSplit(g, nameFont, name, item.SampleValue, available, out nameText, out valueText);

        g.DrawString(nameText, nameFont, nameBrush, nameX, rect.Y + 4);

        if (valueText.Length > 0)
        {
            using var valueBrush = new SolidBrush(Color.FromArgb(120, 128, 138));
            var vx = nameX + g.MeasureString(nameText, nameFont).Width + 4;
            g.DrawString(valueText, ValueFont, valueBrush, vx, rect.Y + 5);
        }

        // Separador sutil
        using var linePen = new Pen(Color.FromArgb(238, 240, 243));
        g.DrawLine(linePen, 4, rect.Bottom - 1, Width - 4, rect.Bottom - 1);
    }

    private static void MeasureSplit(Graphics g, Font nameFont, string name, string? value,
        float available, out string nameText, out string valueText)
    {
        var preview = ToPreview(value);
        valueText = string.IsNullOrWhiteSpace(preview) ? string.Empty : "=  " + preview;
        var nameW = g.MeasureString(name, nameFont).Width;
        var valueW = valueText.Length > 0 ? g.MeasureString(valueText, ValueFont).Width : 0f;

        if (nameW + valueW <= available)
        {
            nameText = name;
            return;
        }

        // Dar prioridad a la visibilidad del nombre; recortar el valor si no cabe.
        valueText = string.Empty;
        while (valueText.Length == 0 && name.Length > 1)
        {
            name = name[..^1];
            if (g.MeasureString(name + "…", nameFont).Width <= available)
            {
                nameText = name + "…";
                return;
            }
        }

        nameText = name;
    }

    private static string ToPreview(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var trimmed = value.TrimStart();
        return trimmed.Length <= ValuePreviewMax ? trimmed : trimmed[..ValuePreviewMax] + "…";
    }

    private static Color StatusColor(ExplorerStatus status) => status switch
    {
        ExplorerStatus.Ok => Color.FromArgb(34, 164, 93),
        ExplorerStatus.NoData => Color.FromArgb(230, 162, 60),
        _ => Color.FromArgb(192, 57, 43)
    };

    private static readonly Font GlyphFont = new("Segoe UI Symbol", 8f, FontStyle.Bold);
    private static readonly Font ValueFont = new("Segoe UI", 8f, FontStyle.Regular);
    private static readonly Font NameFontRegular = new("Consolas", 8.5f, FontStyle.Regular);
    private static readonly Font NameFontBold = new("Consolas", 8.5f, FontStyle.Bold);
    private static readonly Font GroupFont = new("Segoe UI", 8f, FontStyle.Bold);

    private const int ValuePreviewMax = 40;

    private static void DrawStatusGlyph(Graphics g, ExplorerStatus status, float y)
    {
        var text = status switch
        {
            ExplorerStatus.Ok => "✓",
            ExplorerStatus.NoData => "⚠",
            _ => "✕"
        };
        using var brush = new SolidBrush(Color.White);
        g.DrawString(text, GlyphFont, brush, 8.5f, y - 2);
    }

    /* ── Drag de campos ────────────────────────────────────── */
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;

        _downPoint = e.Location;
        _pendingIndex = IndexAt(e.Location.Y - AutoScrollPosition.Y);
        Focus();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        _pendingIndex = -1;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (_pendingIndex < 0)
        {
            Cursor = IndexAt(e.Location.Y - AutoScrollPosition.Y) >= 0 ? Cursors.Hand : Cursors.Default;
            return;
        }

        var delta = new Size(e.X - _downPoint.X, e.Y - _downPoint.Y);
        var dragSize = SystemInformation.DragSize;
        if (Math.Abs(delta.Width) < dragSize.Width && Math.Abs(delta.Height) < dragSize.Height)
            return;

        var index = _pendingIndex;
        _pendingIndex = -1;

        if (index < 0 || index >= _items.Count) return;

        var field = _items[index];
        DragPreview.Show(field.Name + "   (suelta sobre la banda)", Cursor.Position);
        GiveFeedback += OnDragGiveFeedback;
        try
        {
            DoDragDrop(field.DragData, DragDropEffects.Copy);
        }
        finally
        {
            GiveFeedback -= OnDragGiveFeedback;
            DragPreview.Hide();
        }
    }

    private void OnDragGiveFeedback(object? sender, GiveFeedbackEventArgs e)
    {
        // Mantener el cursor por defecto de copia y mover la etiqueta flotante.
        DragPreview.Move(Cursor.Position);
    }
}
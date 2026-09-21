using PdfQuickDebug.Designer.CodeGen;

namespace PdfQuickDebug.Designer.WinForms;

/// <summary>
/// Explorador "Data Source" (estilo Crystal Reports): árbol jerárquico por tablas
/// (Documento, Emisor, Receptor, Detalle, Totales, Adicional, DTO...) con buscador.
/// Los campos provienen del modelo parseado (siempre resolubles en el template
/// generado) y se arrastran al lienzo soltando un <see cref="FieldDragData"/>.
/// </summary>
public sealed class DataSourceExplorerPanel : UserControl
{
    private const int ImgOk = 0;
    private const int ImgNoData = 1;
    private const int ImgDto = 2;
    private const int ImgRaw = 3;

    private readonly TextBox _search = new();
    private readonly TreeView _tree = new();
    private IReadOnlyList<SchemaField> _schema = Array.Empty<SchemaField>();
    private IReadOnlyList<ExplorerField> _rawXml = Array.Empty<ExplorerField>();
    private Point _downPoint;

    public DataSourceExplorerPanel()
    {
        BackColor = Color.White;
        Dock = DockStyle.Fill;

        _search = new TextBox
        {
            Dock = DockStyle.Top,
            PlaceholderText = "🔍  Buscar campo o valor...",
            Font = new Font("Segoe UI", 9f)
        };
        _search.TextChanged += (_, _) => Rebuild();

        _tree = new TreeView
        {
            Dock = DockStyle.Fill,
            ImageList = BuildImages(),
            ShowNodeToolTips = true,
            HideSelection = false,
            Font = new Font("Segoe UI", 9f),
            ItemHeight = 22,
            BorderStyle = BorderStyle.None
        };

        Controls.Add(_tree);
        Controls.Add(_search);

        _tree.MouseDown += OnTreeMouseDown;
        _tree.MouseMove += OnTreeMouseMove;
    }

    /// <summary>Alimenta el árbol: schema del modelo/DTO + nodo "XML crudo" de referencia.</summary>
    public void SetSchema(IReadOnlyList<SchemaField> schema, IReadOnlyList<ExplorerField>? rawXml)
    {
        _schema = schema ?? Array.Empty<SchemaField>();
        _rawXml = rawXml ?? Array.Empty<ExplorerField>();
        Rebuild();
    }

    private void Rebuild()
    {
        var query = _search.Text.Trim();
        var searching = query.Length > 0;

        _tree.BeginUpdate();
        _tree.Nodes.Clear();

        foreach (var group in ModelSchemaCatalog.RootGroups())
        {
            var items = _schema.Where(f => f.Group == group).OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase).ToList();
            if (items.Count == 0) continue;
            AddGroup(_tree, group, items, searching, query);
        }

        foreach (var group in _schema.Select(f => f.Group).Distinct(StringComparer.Ordinal)
                     .Where(g => !ModelSchemaCatalog.RootGroups().Contains(g)))
        {
            var items = _schema.Where(f => f.Group == group).OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase).ToList();
            AddGroup(_tree, group, items, searching, query);
        }

        if (_rawXml.Count > 0)
        {
            var rawRoot = new TreeNode($"XML crudo (referencia — ⚠ usa ➕ XSLT para mapear)  ({_rawXml.Count})")
            {
                ForeColor = Color.FromArgb(120, 128, 138),
                ImageIndex = ImgRaw,
                SelectedImageIndex = ImgRaw
            };
            foreach (var field in _rawXml.Where(f => !searching || Matches(f, query)))
                rawRoot.Nodes.Add(RawNode(field));
            if (!searching || rawRoot.Nodes.Count > 0)
            {
                _tree.Nodes.Add(rawRoot);
                rawRoot.Collapse();
            }
        }

        if (_schema.Count == 0 && _rawXml.Count == 0)
        {
            var hint = new TreeNode("⚠ Sin datos del pipeline para este cliente.")
            {
                ForeColor = Color.FromArgb(150, 115, 40),
                ImageIndex = ImgRaw,
                SelectedImageIndex = ImgRaw,
                ToolTipText = "Falta raw.xml / transformed.xml en data\\work\\<cliente>. Carga un XML con 📂 Muestra XML o crea el cliente cargando un XML."
            };
            _tree.Nodes.Add(hint);
        }

        _tree.EndUpdate();
    }

    private void AddGroup(TreeView tree, string group, List<SchemaField> items, bool searching, string query)
    {
        var root = new TreeNode($"  {group.ToUpperInvariant()}  ({items.Count})")
        {
            ForeColor = Color.FromArgb(60, 70, 85),
            ImageIndex = -1,
            SelectedImageIndex = -1
        };

        foreach (var field in items)
        {
            if (searching && !Matches(field, query))
                continue;
            root.Nodes.Add(FieldNode(field));
        }

        if (!searching || root.Nodes.Count > 0)
        {
            tree.Nodes.Add(root);
            if (searching) root.Expand();
        }
    }

    private static TreeNode FieldNode(SchemaField field)
    {
        var statusIndex = field.RequiresDtoMap
            ? ImgDto
            : field.HasValue ? ImgOk : ImgNoData;

        var color = field.RequiresDtoMap
            ? Color.FromArgb(184, 134, 11)
            : field.HasValue ? Color.FromArgb(31, 35, 40) : Color.FromArgb(150, 115, 40);

        return new TreeNode(FieldLabel(field.Name, field.SampleValue, field.RequiresDtoMap))
        {
            Tag = ToDragData(field),
            ImageIndex = statusIndex,
            SelectedImageIndex = statusIndex,
            ForeColor = color,
            ToolTipText = BuildTooltip(field)
        };
    }

    private static TreeNode RawNode(ExplorerField field)
    {
        var mapped = field.Status == ExplorerStatus.Ok;
        return new TreeNode(FieldLabel(field.Name, field.SampleValue, !mapped))
        {
            Tag = field.DragData,
            ImageIndex = ImgRaw,
            SelectedImageIndex = ImgRaw,
            ForeColor = mapped ? Color.FromArgb(34, 164, 93) : Color.FromArgb(192, 57, 43),
            ToolTipText = "XPath: " + field.XPath +
                          (string.IsNullOrWhiteSpace(field.Expr)
                              ? "\n⚠ sin mapeo directo — usa ➕ XSLT para inyectarlo como CustomField"
                              : "\nExpr: " + field.Expr)
        };
    }

    private static FieldDragData ToDragData(SchemaField field) => new()
    {
        Name = field.Name,
        XPath = field.XPath ?? string.Empty,
        RelativePath = field.XPath ?? string.Empty,
        Expr = field.Expr,
        Default = field.HasValue ? field.SampleValue : string.Empty,
        SampleValue = field.SampleValue,
        IsRepeated = field.IsRepeated
    };

    private static bool Matches(string name, string? expr, string? value, string? xpath, string query)
        => name.Contains(query, StringComparison.OrdinalIgnoreCase)
           || (expr ?? string.Empty).Contains(query, StringComparison.OrdinalIgnoreCase)
           || (value ?? string.Empty).Contains(query, StringComparison.OrdinalIgnoreCase)
           || (xpath ?? string.Empty).Contains(query, StringComparison.OrdinalIgnoreCase);

    private static bool Matches(SchemaField field, string query)
        => Matches(field.Name, field.Expr, field.SampleValue, field.XPath, query);

    private static bool Matches(ExplorerField field, string query)
        => Matches(field.Name, field.Expr, field.SampleValue, field.XPath, query);

    private static string FieldLabel(string name, string? sample, bool mark)
    {
        var label = name;
        if (!string.IsNullOrWhiteSpace(sample))
            label += "   =  " + sample;
        if (mark)
            label += "   ⚠";
        return label;
    }

    private static string BuildTooltip(SchemaField field)
    {
        var tip = "Expr: " + field.Expr;
        if (!string.IsNullOrWhiteSpace(field.XPath))
            tip += "\nXPath: " + field.XPath;
        if (!string.IsNullOrWhiteSpace(field.Note))
            tip += "\n" + field.Note;
        if (field.RequiresDtoMap)
            tip += "\n⚠ solo se completa si el servicio lo mapea en MapToCustomDto";
        if (field.IsLine)
            tip += "\n(línea del detalle — solo sobre una banda Detail)";
        return tip;
    }

    /* ── Drag de campos ────────────────────────────────────── */
    private void OnTreeMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        _downPoint = e.Location;
        _tree.Focus();

        var node = _tree.GetNodeAt(e.Location);
        if (node != null)
            _tree.SelectedNode = node;
    }

    private void OnTreeMouseMove(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;

        var delta = new Size(e.X - _downPoint.X, e.Y - _downPoint.Y);
        if (Math.Abs(delta.Width) < SystemInformation.DragSize.Width &&
            Math.Abs(delta.Height) < SystemInformation.DragSize.Height)
            return;

        var node = _tree.GetNodeAt(_downPoint);
        if (node?.Tag is not FieldDragData data || string.IsNullOrEmpty(data.Name))
            return;

        DragPreview.Show(data.Name + "   (suelta sobre la banda)", Cursor.Position);
        GiveFeedback += OnDragGiveFeedback;
        try
        {
            DoDragDrop(data, DragDropEffects.Copy);
        }
        finally
        {
            GiveFeedback -= OnDragGiveFeedback;
            DragPreview.Hide();
        }
    }

    private void OnDragGiveFeedback(object? sender, GiveFeedbackEventArgs e)
    {
        DragPreview.Move(Cursor.Position);
    }

    /* ── Estado (badges del árbol) ──────────────────────────── */
    private static ImageList BuildImages()
    {
        var list = new ImageList { ImageSize = new Size(14, 14), ColorDepth = ColorDepth.Depth32Bit };
        list.Images.Add(StatusGlyph(Color.FromArgb(34, 164, 93)));
        list.Images.Add(StatusGlyph(Color.FromArgb(230, 162, 60)));
        list.Images.Add(StatusGlyph(Color.FromArgb(184, 134, 11)));
        list.Images.Add(StatusGlyph(Color.FromArgb(170, 178, 190)));
        return list;
    }

    private static Bitmap StatusGlyph(Color color)
    {
        var bmp = new Bitmap(14, 14);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(color);
        g.FillEllipse(brush, 1, 1, 12, 12);
        return bmp;
    }
}
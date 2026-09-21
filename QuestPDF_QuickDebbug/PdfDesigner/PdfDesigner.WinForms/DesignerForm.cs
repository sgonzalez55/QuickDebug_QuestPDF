using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml;
using PdfQuickDebug.Core;
using PdfQuickDebug.Designer.CodeGen;
using PdfQuickDebug.Designer.Data;
using PdfQuickDebug.Designer.Model;
using PdfQuickDebug.Designer.Rendering;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Color = System.Drawing.Color;
using Image = System.Drawing.Image;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace PdfQuickDebug.Designer.WinForms;

/// <summary>
/// Editor visual compositivo: árbol de nodos (Column/Row/Table/Container/…),
/// panel de propiedades por tipo, explorador de campos con drag & drop y
/// preview en vivo renderizado con QuestPDF.
/// </summary>
public sealed class DesignerForm : Form
{
    private const string OutDir = @"C:\Users\santiago.gonzalez\Downloads\QuestPDF_QuickDebbug_v3\QuestPDF_QuickDebbug\PdfDesigner\output";
    private const string GeneratedDir = OutDir + @"\Generated";

    private ReportDesign _design = new();
    private string _xmlText = string.Empty;
    private readonly string _clientName;
    private readonly string _workDir;
    private string? _activeDesignPath;
    private DataContract? _dataContract;

    private readonly Stack<string> _undo = new();
    private readonly Stack<string> _redo = new();

    private TreeView _tree = null!;
    private CompositionCanvas _canvas = null!;
    private FlowLayoutPanel _propsHost = null!;
    private FlowLayoutPanel _previewPages = null!;
    private ToolStripStatusLabel _status = null!;
    private FieldExplorerPanel _fieldExplorer = null!;
    private DataSourceExplorerPanel _dataSource = null!;
    private Panel _dataHost = null!;
    private TableColumn? _editingColumn;

    private Point _treeDownPoint;
    private TreeNode? _treeDownNode;

    public DesignerForm(string clientName, StartAction mode, string chosenPath, string? onboardXmlPath)
    {
        _clientName = clientName;
        _workDir = ClientData.WorkDir(clientName);

        Text = $"Diseñador QuestPDF — {clientName}";
        Width = 1280;
        Height = 800;
        MinimumSize = new Size(980, 620);
        Font = new Font("Segoe UI", 9f);
        BackColor = Color.FromArgb(244, 246, 249);

        BuildUi();
        ApplyClient(mode, chosenPath, onboardXmlPath);
    }

    /* ── UI ────────────────────────────────────────────────── */

    private void BuildUi()
    {
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, WrapContents = false, BackColor = Color.FromArgb(238, 241, 245), Padding = new Padding(8, 5, 8, 5) };
        toolbar.Controls.Add(Button("▶ Render", RenderPreview, "Renderiza la vista previa"));
        toolbar.Controls.Add(Button("💾 PDF", RenderPdf, "Genera el PDF y lo abre"));
        toolbar.Controls.Add(Button("⧉ C#", GenerateCSharp, "Genera el template C# del diseño"));
        toolbar.Controls.Add(Button("⤒ Guardar", SaveDesign, "Guarda el diseño JSON"));
        toolbar.Controls.Add(Button("⇪ Importar", ImportJson, "Importa un diseño JSON"));
        toolbar.Controls.Add(Button("⇊ Exportar", ExportJson, "Exporta el diseño JSON"));
        toolbar.Controls.Add(Button("▣ Página", PageSetup, "Configura tamaño y márgenes"));
        toolbar.Controls.Add(Button("⤺", Undo, "Deshacer"));
        toolbar.Controls.Add(Button("⤻", Redo, "Rehacer"));
        toolbar.Controls.Add(Button("📂 XML", LoadSampleXml, "Cargar XML de muestra"));

        var leftPanel = new Panel { Dock = DockStyle.Left, Width = 260, BackColor = Color.White };
        var rightPanel = new Panel { Dock = DockStyle.Right, Width = 340, BackColor = Color.White };

        var previewHost = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.FromArgb(224, 227, 232), AllowDrop = true };
        previewHost.DragEnter += OnDragEnter;
        previewHost.DragOver += OnDragOver;
        previewHost.DragDrop += OnPreviewDragDrop;
        _previewPages = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = true, Padding = new Padding(12) };
        previewHost.Controls.Add(_previewPages);

        // Centro: pestañas "Diseño" (lienzo WYSIWYG) y "Preview" (PDF real).
        _canvas = new CompositionCanvas();
        _canvas.SelectionChanged += (_, _) => OnCanvasSelectionChanged();
        _canvas.DragEnter += OnDragEnter;
        _canvas.DragOver += OnDragOver;
        _canvas.DragDrop += OnCanvasDragDrop;
        var designScroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.FromArgb(232, 235, 240) };
        designScroll.Controls.Add(_canvas);

        var centerTabs = new TabControl { Dock = DockStyle.Fill };
        var tabDesign = new TabPage("Diseño") { BackColor = Color.White };
        tabDesign.Controls.Add(designScroll);
        var tabPreview = new TabPage("Preview") { BackColor = Color.White };
        tabPreview.Controls.Add(previewHost);
        centerTabs.TabPages.Add(tabDesign);
        centerTabs.TabPages.Add(tabPreview);

        BuildLeftPanel(leftPanel);
        BuildRightPanel(rightPanel);

        var status = new StatusStrip();
        _status = new ToolStripStatusLabel("Listo");
        status.Items.Add(_status);

        Controls.Add(centerTabs);
        Controls.Add(rightPanel);
        Controls.Add(leftPanel);
        Controls.Add(toolbar);
        Controls.Add(status);
    }

    private void BuildLeftPanel(Panel leftPanel)
    {
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            BackColor = Color.White,
            SplitterWidth = 4
        };

        // ── Arriba: estructura (paleta + árbol), siempre visible ──
        var structPanel = new Panel { Dock = DockStyle.Fill };
        var palette = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = true, Padding = new Padding(6) };
        foreach (var (type, label) in NodeFactory.Palette())
        {
            var b = new Button { Text = label, AutoSize = true, Margin = new Padding(2), FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            var down = new Point(-1, -1);
            b.MouseDown += (_, e) => { if (e.Button == MouseButtons.Left) down = e.Location; };
            b.MouseMove += (_, e) =>
            {
                if (e.Button != MouseButtons.Left || down.X < 0) return;
                if (Math.Abs(e.X - down.X) < SystemInformation.DragSize.Width &&
                    Math.Abs(e.Y - down.Y) < SystemInformation.DragSize.Height)
                    return;
                down = new Point(-1, -1);
                b.DoDragDrop(new NodeDragData { Type = type }, DragDropEffects.Copy);
            };
            b.Click += (_, _) => AddNode(type);
            palette.Controls.Add(b);
        }

        _tree = new TreeView { Dock = DockStyle.Fill, HideSelection = false, FullRowSelect = true, AllowDrop = true };
        _tree.AfterSelect += (_, _) => { _canvas.Select(SelectedNode()); BuildProperties(); };
        _tree.KeyDown += OnTreeKeyDown;
        _tree.MouseDown += OnTreeMouseDown;
        _tree.MouseMove += OnTreeMouseMove;
        _tree.DragEnter += OnDragEnter;
        _tree.DragOver += OnDragOver;
        _tree.DragDrop += OnTreeDragDrop;

        structPanel.Controls.Add(_tree);
        structPanel.Controls.Add(palette);

        // ── Abajo: datos (Data Source + Campos XML), siempre visible ──
        var dataPanel = new Panel { Dock = DockStyle.Fill };
        _dataSource = new DataSourceExplorerPanel { Dock = DockStyle.Fill };
        _fieldExplorer = new FieldExplorerPanel { Dock = DockStyle.Fill };
        _dataHost = new Panel { Dock = DockStyle.Fill };
        _dataHost.Controls.Add(_dataSource);
        var dataTabs = new TabControl { Dock = DockStyle.Fill };
        var tSource = new TabPage("Data Source") { BackColor = Color.White };
        tSource.Controls.Add(_dataHost);
        var tFields = new TabPage("Campos XML") { BackColor = Color.White };
        tFields.Controls.Add(_fieldExplorer);
        dataTabs.TabPages.Add(tSource);
        dataTabs.TabPages.Add(tFields);
        dataPanel.Controls.Add(dataTabs);

        split.Panel1.Controls.Add(structPanel);
        split.Panel2.Controls.Add(dataPanel);
        split.SplitterDistance = 260;

        leftPanel.Controls.Add(split);
    }

    private void BuildRightPanel(Panel rightPanel)
    {
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
        _propsHost = new FlowLayoutPanel { Dock = DockStyle.Top, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(12) };
        scroll.Controls.Add(_propsHost);
        rightPanel.Controls.Add(scroll);
    }

    private static Button Button(string text, Action onClick, string? tip = null)
    {
        var b = new Button { Text = text, AutoSize = true, Margin = new Padding(2), FlatStyle = FlatStyle.Flat };
        b.Click += (_, _) => onClick();
        if (tip != null)
            new ToolTip().SetToolTip(b, tip);
        return b;
    }

    /* ── Cliente y diseño ──────────────────────────────────── */

    private void ApplyClient(StartAction mode, string chosenPath, string? onboardXmlPath)
    {
        var cfg = ClientData.LoadConfig(_clientName);
        _design = new ReportDesign
        {
            Metadata = new DesignMetadata
            {
                Nit = cfg.Nit,
                ClientName = string.IsNullOrWhiteSpace(cfg.ClientName) ? _clientName : cfg.ClientName,
                TemplateName = SanitizeName(Path.GetFileNameWithoutExtension(chosenPath))
            }
        };

        OnboardClientXml(onboardXmlPath);

        _dataContract = DataContract.Load(ClientData.DataContractPath(_clientName));

        if (mode == StartAction.Cargar && File.Exists(chosenPath))
        {
            try
            {
                _design = DesignerJson.Load(chosenPath);
                _activeDesignPath = chosenPath;
            }
            catch
            {
                SetStatus("El diseño anterior no es compatible con este modelo; se inició uno nuevo.");
            }
        }

        LoadXml();

        _canvas.Design = _design;
        _canvas.Resolver = CreateResolver();

        RefreshTree();
        try
        {
            RefreshDataSource();
            RenderDataFields();
            RenderPreview();
        }
        catch (Exception ex)
        {
            SetStatus("No se pudieron cargar los datos: " + ex.Message);
        }
    }

    /// <summary>Guarda el XML de un cliente nuevo, corre el pipeline y regenera el contrato.</summary>
    private void OnboardClientXml(string? onboardXmlPath)
    {
        if (string.IsNullOrWhiteSpace(onboardXmlPath) || !File.Exists(onboardXmlPath)) return;

        try
        {
            var rawPath = ClientData.RawXmlPath(_clientName);
            Directory.CreateDirectory(Path.GetDirectoryName(rawPath)!);
            File.Copy(onboardXmlPath, rawPath, overwrite: true);

            var map = BindingMap.Load(ClientData.BindingMapPath(_clientName));
            var contract = DataContract.Build(map, rawPath, ClientData.XsltPath(_clientName));
            contract.Save(ClientData.DataContractPath(_clientName));

            var (_, transformed) = ModelSchemaCatalog.LoadPipeline(rawPath, ClientData.XsltPath(_clientName));
            if (!string.IsNullOrWhiteSpace(transformed))
                File.WriteAllText(ClientData.TransformedXmlPath(_clientName), "<Root>" + transformed + "</Root>");
        }
        catch (Exception ex)
        {
            SetStatus("No se pudo procesar el XML del cliente: " + ex.Message);
        }
    }

    private static string SanitizeName(string name)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var ch in name)
            sb.Append(char.IsLetterOrDigit(ch) ? ch : '_');
        var r = sb.ToString().Trim('_');
        return string.IsNullOrEmpty(r) ? "Report" : r;
    }

    private void LoadXml()
    {
        _xmlText = string.Empty;
        var transformed = ClientData.TransformedXmlPath(_clientName);
        if (File.Exists(transformed))
        {
            // El transformed.xml ya es un documento válido: <Root> envuelve los
            // elementos hermanos (CFD, Folio, Emisor, ...). NO quitar el wrapper.
            _xmlText = File.ReadAllText(transformed).Trim();
        }
        else
        {
            var raw = ClientData.RawXmlPath(_clientName);
            if (File.Exists(raw))
                _xmlText = File.ReadAllText(raw);
        }

        MergeNamespaces(_xmlText);
    }

    /// <summary>Registra los prefijos xmlns del XML en el diseño (para XPath con cac:/cbc:).</summary>
    private void MergeNamespaces(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml)) return;
        try
        {
            var doc = new XmlDocument { XmlResolver = null };
            doc.LoadXml(xml);
            foreach (XmlNode node in doc.SelectNodes("//*")!)
            {
                if (node.Attributes == null) continue;
                foreach (XmlAttribute attr in node.Attributes)
                {
                    if (attr.Name.StartsWith("xmlns:", StringComparison.OrdinalIgnoreCase))
                        _design.Namespaces[attr.Name.Substring(6)] = attr.Value;
                }
            }
        }
        catch { }
    }

    private XmlDataResolver? CreateResolver()
    {
        if (string.IsNullOrWhiteSpace(_xmlText)) return null;
        try
        {
            return new XmlDataResolver(_xmlText, _design.Namespaces);
        }
        catch
        {
            return null;
        }
    }

    private InvoiceModel? LoadClientModel(out string fuente)
    {
        fuente = "sin datos";
        var raw = ClientData.RawXmlPath(_clientName);
        var xslt = ClientData.XsltPath(_clientName);
        if (File.Exists(raw) && File.Exists(xslt))
        {
            var (model, _) = ModelSchemaCatalog.LoadPipeline(raw, xslt);
            if (model != null) { fuente = "pipeline (raw.xml → XSLT)"; return model; }
            fuente = "pipeline falló";
        }
        var transformed = ClientData.TransformedXmlPath(_clientName);
        if (File.Exists(transformed))
        {
            var (model, _) = ModelSchemaCatalog.LoadTransformed(transformed);
            if (model != null) { fuente = "transformed.xml"; return model; }
        }
        return null;
    }

    private void RefreshDataSource()
    {
        try
        {
            var model = LoadClientModel(out var fuente);
            var fields = new List<SchemaField>();
            if (model != null) fields.AddRange(ModelSchemaCatalog.BuildModel(model));
            fields.AddRange(ModelSchemaCatalog.BuildDto(ClientData.DtoPath(_clientName)));

            var raw = new List<ExplorerField>();
            if (!string.IsNullOrWhiteSpace(_xmlText))
            {
                try
                {
                    var root = XmlFieldCatalog.Build(_xmlText);
                    foreach (var child in root.Children)
                        FlattenXmlNode(child, raw, CreateResolver());
                }
                catch { }
            }

            _dataSource.SetSchema(fields, raw);
            SetStatus($"Data Source: {fields.Count} campos ({fuente}) + {raw.Count} XML crudo");
        }
        catch (Exception ex)
        {
            SetStatus("Data Source: " + ex.Message);
        }
    }

    private void RenderDataFields()
    {
        var fields = new List<ExplorerField>();
        var resolver = CreateResolver();
        if (_dataContract != null)
            foreach (var f in _dataContract.Fields)
                fields.Add(BuildContractField(f, resolver));
        if (!string.IsNullOrWhiteSpace(_xmlText))
        {
            try
            {
                var root = XmlFieldCatalog.Build(_xmlText);
                foreach (var child in root.Children)
                    FlattenXmlNode(child, fields, resolver);
            }
            catch { }
        }
        _fieldExplorer.SetFields(fields);
    }

    private static ExplorerField BuildContractField(DataContract.Field field, XmlDataResolver? resolver)
    {
        var sample = resolver?.ResolveString(field.Xpath, string.Empty) ?? string.Empty;
        var status = string.IsNullOrWhiteSpace(sample)
            ? string.IsNullOrWhiteSpace(field.Expr) ? ExplorerStatus.NoMapping : ExplorerStatus.NoData
            : ExplorerStatus.Ok;
        return new ExplorerField
        {
            Group = field.Group,
            Name = field.Name,
            XPath = field.Xpath,
            RelativePath = field.Xpath,
            Expr = field.Expr,
            SampleValue = string.IsNullOrWhiteSpace(sample) ? null : sample,
            Status = status,
            DragData = new FieldDragData
            {
                Name = field.Name, XPath = field.Xpath, RelativePath = field.Xpath,
                Expr = string.IsNullOrWhiteSpace(field.Expr) ? null : field.Expr,
                SampleValue = string.IsNullOrWhiteSpace(sample) ? null : sample
            }
        };
    }

    private void FlattenXmlNode(XmlFieldCatalog.FieldNode node, List<ExplorerField> fields, XmlDataResolver? resolver)
    {
        if (node.IsRepeated && node.Children.Count > 0)
        {
            fields.Add(new ExplorerField
            {
                Group = "XML", Name = node.Name, XPath = node.XPath, RelativePath = node.RelativePath,
                Expr = ResolveFieldExpr(node), IsRepeated = true, Status = ExplorerStatus.Ok,
                DragData = ToDragData(node)
            });
        }
        else if (node.IsField)
        {
            var sample = node.Value ?? resolver?.ResolveString(node.XPath, string.Empty) ?? string.Empty;
            var expr = ResolveFieldExpr(node);
            fields.Add(new ExplorerField
            {
                Group = "XML", Name = node.Name, XPath = node.XPath, RelativePath = node.RelativePath,
                Expr = expr,
                SampleValue = string.IsNullOrWhiteSpace(sample) ? null : sample,
                Status = string.IsNullOrWhiteSpace(sample)
                    ? string.IsNullOrWhiteSpace(expr) ? ExplorerStatus.NoMapping : ExplorerStatus.NoData
                    : ExplorerStatus.Ok,
                DragData = ToDragData(node)
            });
        }
        foreach (var child in node.Children)
            FlattenXmlNode(child, fields, resolver);
    }

    private FieldDragData ToDragData(XmlFieldCatalog.FieldNode field)
    {
        var data = new FieldDragData
        {
            Name = field.Name, XPath = field.XPath, RelativePath = field.RelativePath,
            Expr = ResolveFieldExpr(field), SampleValue = field.Value, IsRepeated = field.IsRepeated
        };
        foreach (var child in field.Children.Where(c => c.IsField))
        {
            data.Children.Add(new FieldDragData
            {
                Name = child.Name, XPath = child.XPath, RelativePath = child.RelativePath,
                Expr = ResolveFieldExpr(child), SampleValue = child.Value
            });
        }
        return data;
    }

    private string? ResolveFieldExpr(XmlFieldCatalog.FieldNode field)
    {
        if (field.XPath.StartsWith("//CustomField[@Name=", StringComparison.Ordinal) ||
            field.XPath.StartsWith("//Campo[@clave=", StringComparison.Ordinal) ||
            field.XPath.StartsWith("//Adicional/Campo[@clave=", StringComparison.Ordinal))
            return $"GetCustomField(model, \"{field.Name}\")";
        if (_dataContract == null) return null;
        var target = NormalizeXPath(field.XPath);
        foreach (var candidate in _dataContract.Fields)
        {
            if (string.IsNullOrWhiteSpace(candidate.Xpath)) continue;
            var path = NormalizeXPath(candidate.Xpath);
            if (path == target || target.EndsWith(path, StringComparison.Ordinal) || path.EndsWith(target, StringComparison.Ordinal))
                return candidate.Expr;
        }
        return null;
    }

    private static string NormalizeXPath(string xpath)
    {
        var s = Regex.Replace(xpath, @"[A-Za-z_][\w.-]*:", string.Empty);
        s = s.Replace("//", "/").Trim('/');
        return Regex.Replace(s, @"\[\d+\]", string.Empty);
    }

    /* ── Árbol de nodos ────────────────────────────────────── */

    private void RefreshTree()
    {
        var selected = SelectedNode();
        _tree.BeginUpdate();
        _tree.Nodes.Clear();
        _tree.Nodes.Add(SlotNode("Header", _design.Header));
        _tree.Nodes.Add(SlotNode("Content", _design.Content));
        _tree.Nodes.Add(SlotNode("Footer", _design.Footer));
        _tree.ExpandAll();
        _tree.EndUpdate();
        SelectInTree(selected);
        _canvas.Rebuild();
    }

    private static TreeNode SlotNode(string slot, LayoutNode root)
    {
        var n = new TreeNode($"{slot}  ({root.Children.Count})") { Tag = slot };
        foreach (var child in root.Children)
            n.Nodes.Add(NodeTreeNode(child));
        return n;
    }

    private static TreeNode NodeTreeNode(LayoutNode node)
    {
        var n = new TreeNode(NodeLabel(node)) { Tag = node };
        foreach (var child in node.Children)
            n.Nodes.Add(NodeTreeNode(child));
        return n;
    }

    private static string NodeLabel(LayoutNode node)
    {
        var label = node.Type switch
        {
            "column" => "Columna",
            "row" => "Fila",
            "container" => "Contenedor",
            "table" => "Tabla",
            "spacer" => "Espacio",
            "pageBreak" => "Salto",
            "text" => "Texto: " + Short(node.Text, 20),
            "field" => "Campo: " + Short(node.Binding?.Xpath ?? node.Binding?.Expr ?? "—", 20),
            "image" => "Imagen",
            "barcode" => "Código: " + (node.Symbology ?? "—"),
            "line" => "Línea",
            _ => node.Type
        };
        if (!string.IsNullOrWhiteSpace(node.Name)) label += $"  ({node.Name})";
        return label;
    }

    private static string Short(string? value, int max)
        => string.IsNullOrEmpty(value) ? string.Empty : (value.Length > max ? value[..max] + "…" : value);

    private LayoutNode? SelectedNode() => _tree.SelectedNode?.Tag as LayoutNode;

    private string? SelectedSlot() => _tree.SelectedNode?.Tag as string;

    private void SelectInTree(LayoutNode? node)
    {
        if (node == null) return;
        foreach (TreeNode tn in _tree.Nodes)
        {
            if (ReferenceEquals(tn.Tag, node)) { _tree.SelectedNode = tn; return; }
            foreach (TreeNode child in tn.Nodes)
                if (ReferenceEquals(child.Tag, node)) { _tree.SelectedNode = child; return; }
        }
    }

    private void OnTreeKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Delete) { RemoveNode(); e.Handled = true; }
        else if (e.Control && e.KeyCode == Keys.Up) { MoveNode(-1); e.Handled = true; }
        else if (e.Control && e.KeyCode == Keys.Down) { MoveNode(+1); e.Handled = true; }
        else if (e.Control && e.KeyCode == Keys.D) { DuplicateNode(); e.Handled = true; }
    }

    private List<LayoutNode> SlotChildren(string slot) => slot switch
    {
        "Header" => _design.Header.Children,
        "Footer" => _design.Footer.Children,
        _ => _design.Content.Children
    };

    private List<LayoutNode>? FindParentList(LayoutNode node)
    {
        foreach (var slot in new[] { "Header", "Content", "Footer" })
        {
            var root = slot switch
            {
                "Header" => _design.Header,
                "Footer" => _design.Footer,
                _ => _design.Content
            };
            var result = FindInList(root.Children, node);
            if (result != null) return result;
        }
        return null;
    }

    private List<LayoutNode>? FindInList(List<LayoutNode> list, LayoutNode target)
    {
        foreach (var n in list)
        {
            if (ReferenceEquals(n, target)) return list;
            var result = FindInList(n.Children, target);
            if (result != null) return result;
        }
        return null;
    }

    private void AddNode(string type, LayoutNode? dropTarget = null)
    {
        PushUndo();
        var node = NodeFactory.Create(type);
        ListForDropTarget(dropTarget ?? SelectedNode()).Add(node);
        RefreshTree();
        SelectInTree(node);
        BuildProperties();
    }

    private void RemoveNode()
    {
        var node = SelectedNode();
        if (node == null) return;
        var list = FindParentList(node);
        if (list == null) return;
        PushUndo();
        list.Remove(node);
        _tree.SelectedNode = null;
        RefreshTree();
        BuildProperties();
    }

    private void DuplicateNode()
    {
        var node = SelectedNode();
        if (node == null) return;
        var list = FindParentList(node);
        if (list == null) return;
        PushUndo();
        var clone = System.Text.Json.JsonSerializer.Deserialize<LayoutNode>(
            System.Text.Json.JsonSerializer.Serialize(node, DesignerJson.Options), DesignerJson.Options)!;
        clone.Id = Guid.NewGuid().ToString("N")[..8];
        list.Insert(list.IndexOf(node) + 1, clone);
        RefreshTree();
        SelectInTree(clone);
    }

    private void MoveNode(int delta)
    {
        var node = SelectedNode();
        if (node == null) return;
        var list = FindParentList(node);
        if (list == null) return;
        var idx = list.IndexOf(node);
        var next = idx + delta;
        if (next < 0 || next >= list.Count) return;
        PushUndo();
        list.RemoveAt(idx);
        list.Insert(next, node);
        RefreshTree();
        SelectInTree(node);
    }

    private void OnDragEnter(object? sender, DragEventArgs e) => ApplyDragEffect(e);

    private void OnDragOver(object? sender, DragEventArgs e) => ApplyDragEffect(e);

    private static void ApplyDragEffect(DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(typeof(NodeMoveData)) == true)
            e.Effect = DragDropEffects.Move;
        else if (e.Data?.GetDataPresent(typeof(FieldDragData)) == true ||
                 e.Data?.GetDataPresent(typeof(NodeDragData)) == true)
            e.Effect = DragDropEffects.Copy;
        else
            e.Effect = DragDropEffects.None;
    }

    private void OnTreeMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        _treeDownPoint = e.Location;
        _treeDownNode = _tree.GetNodeAt(e.Location);
    }

    private void OnTreeMouseMove(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || _treeDownNode == null) return;
        if (Math.Abs(e.X - _treeDownPoint.X) < SystemInformation.DragSize.Width &&
            Math.Abs(e.Y - _treeDownPoint.Y) < SystemInformation.DragSize.Height)
            return;

        var node = _treeDownNode;
        _treeDownNode = null;
        if (node.Tag is LayoutNode layoutNode)
            _tree.DoDragDrop(new NodeMoveData { Node = layoutNode }, DragDropEffects.Move);
    }

    private void OnTreeDragDrop(object? sender, DragEventArgs e)
    {
        var target = _tree.GetNodeAt(_tree.PointToClient(new Point(e.X, e.Y)))?.Tag as LayoutNode;
        DispatchDrop(e.Data, target);
    }

    private void OnPreviewDragDrop(object? sender, DragEventArgs e)
        => DispatchDrop(e.Data, null);

    private void OnCanvasDragDrop(object? sender, DragEventArgs e)
    {
        var target = _canvas.NodeAt(_canvas.PointToClient(new Point(e.X, e.Y)));
        DispatchDrop(e.Data, target);
    }

    private void OnCanvasSelectionChanged()
    {
        BuildProperties();
        SelectInTree(_canvas.SelectedNode);
    }

    private void DispatchDrop(IDataObject? data, LayoutNode? target)
    {
        if (data?.GetData(typeof(FieldDragData)) is FieldDragData field) { AddFieldToDesign(field, target); return; }
        if (data?.GetData(typeof(NodeDragData)) is NodeDragData newNode) { AddNode(newNode.Type, target); return; }
        if (data?.GetData(typeof(NodeMoveData)) is NodeMoveData move) { MoveNodeTo(move.Node, target); return; }
    }

    private void AddFieldToDesign(FieldDragData field, LayoutNode? dropTarget)
    {
        PushUndo();

        LayoutNode node;
        if (field.IsRepeated && field.Children.Count > 0)
        {
            var table = new LayoutNode { Type = "table", RepeatXPath = field.XPath, Name = field.Name };
            foreach (var child in field.Children)
            {
                table.Columns.Add(new TableColumn
                {
                    Header = child.Name,
                    Binding = new BindingDesign { Xpath = child.RelativePath, Expr = child.Expr },
                    Format = "text"
                });
            }
            node = table;
        }
        else
        {
            node = new LayoutNode
            {
                Type = "field",
                Name = field.Name,
                Binding = new BindingDesign { Xpath = field.XPath, Expr = field.Expr, Default = string.Empty },
                Format = "text"
            };
        }

        ListForDropTarget(dropTarget ?? SelectedNode()).Add(node);
        RefreshTree();
        SelectInTree(node);
        BuildProperties();
    }

    /// <summary>Mueve un nodo existente a la posición del destino (hijo de contenedor, hermano antes de hoja, o al slot).</summary>
    private void MoveNodeTo(LayoutNode node, LayoutNode? target)
    {
        if (target != null && (ReferenceEquals(target, node) || IsDescendant(node, target)))
            return; // evita mover el nodo dentro de sí mismo

        var current = FindParentList(node);
        if (current == null) return;

        PushUndo();
        current.Remove(node);

        if (target != null)
        {
            if (target.Type is "column" or "row" or "container")
            {
                target.Children.Add(node);
            }
            else
            {
                var parent = FindParentList(target) ?? SlotChildren(SelectedSlot() ?? "Content");
                var idx = parent.IndexOf(target);
                parent.Insert(idx >= 0 ? idx : parent.Count, node);
            }
        }
        else
        {
            SlotChildren(SelectedSlot() ?? "Content").Add(node);
        }

        RefreshTree();
        SelectInTree(node);
        BuildProperties();
    }

    private static bool IsDescendant(LayoutNode ancestor, LayoutNode node)
    {
        foreach (var child in ancestor.Children)
        {
            if (ReferenceEquals(child, node) || IsDescendant(child, node)) return true;
        }
        return false;
    }

    /// <summary>Lista destino del drop: hijos del contenedor sobre el que se suelta, o el slot seleccionado.</summary>
    private List<LayoutNode> ListForDropTarget(LayoutNode? node)
    {
        if (node != null)
        {
            if (node.Type is "column" or "row" or "container") return node.Children;
            var parent = FindParentList(node);
            if (parent != null) return parent;
        }
        return SlotChildren(SelectedSlot() ?? "Content");
    }

    /* ── Propiedades ───────────────────────────────────────── */

    private void BuildProperties()
    {
        _propsHost.SuspendLayout();
        _propsHost.Controls.Clear();
        _editingColumn = null;

        if (_tree.SelectedNode?.Tag is LayoutNode node)
            BuildNodeProps(node);
        else
            _propsHost.Controls.Add(new Label { Text = "Selecciona un nodo del árbol.", AutoSize = true, ForeColor = Color.Gray });

        _propsHost.ResumeLayout();
    }

    private void BuildNodeProps(LayoutNode node)
    {
        AddHeader(node.Type.ToUpperInvariant());
        AddText("Nombre", node.Name ?? "", v => { node.Name = v; RefreshTree(); });

        switch (node.Type)
        {
            case "column" or "row" or "container" or "table":
                AddBoxProps(node);
                if (node.Type == "container")
                {
                    AddNumber("Alto (pt)", node.Height, v => node.Height = v);
                    AddNumber("Ancho (pt)", node.Width, v => node.Width = v);
                }
                break;
        }

        if (node.Type is "text" or "field" or "image" or "barcode" or "line" or "container")
        {
            AddNumber("Ancho fijo (pt)", node.Width, v => { node.Width = v; if (v != null) node.Weight = null; });
            AddNumber("Peso relativo", node.Weight, v => { node.Weight = v; if (v != null) node.Width = null; });
        }

        switch (node.Type)
        {
            case "text":
                AddText("Texto", node.Text ?? "", v => node.Text = v);
                AddTextStyle(node);
                break;
            case "field":
                AddHeader("VÍNCULO");
                AddText("XPath", node.Binding?.Xpath ?? "", v => { node.Binding ??= new BindingDesign(); node.Binding.Xpath = v; });
                AddText("Expresión C#", node.Binding?.Expr ?? "", v => { node.Binding ??= new BindingDesign(); node.Binding.Expr = v; });
                AddCombo("Formato", new[] { "text", "currency", "currency2", "number", "number2", "upper", "lower" }, node.Format ?? "text", v => node.Format = v);
                AddTextStyle(node);
                break;
            case "image":
                AddText("Origen (ruta/base64)", node.Source ?? "", v => node.Source = v);
                AddNumber("Alto (pt)", node.Height, v => node.Height = v);
                AddNumber("Ancho (pt)", node.Width, v => node.Width = v);
                break;
            case "barcode":
                AddCombo("Simbol.", new[] { "QR", "CODE_128", "CODE_39", "EAN_13" }, node.Symbology ?? "QR", v => node.Symbology = v);
                AddText("XPath", node.Binding?.Xpath ?? "", v => { node.Binding ??= new BindingDesign(); node.Binding.Xpath = v; });
                AddNumber("Alto (pt)", node.Height, v => node.Height = v);
                AddNumber("Ancho (pt)", node.Width, v => node.Width = v);
                break;
            case "line":
                AddCombo("Dirección", new[] { "horizontal", "vertical" }, node.Direction ?? "horizontal", v => node.Direction = v);
                AddNumber("Grosor", node.Thickness, v => node.Thickness = v ?? 1);
                AddColor("Color", node.Style?.Color, v => { node.Style ??= new StyleDesign(); node.Style.Color = v; });
                break;
            case "spacer":
                AddNumber("Alto (pt)", node.SpacerHeight, v => node.SpacerHeight = v ?? 12);
                break;
            case "table":
                AddText("Repeat XPath (filas)", node.RepeatXPath ?? "", v => node.RepeatXPath = v);
                AddBool("Mostrar encabezado", node.ShowHeader, v => node.ShowHeader = v);
                BuildColumnsEditor(node);
                break;
        }

        AddBool("Visible", node.Visible, v => node.Visible = v);
    }

    private void AddBoxProps(LayoutNode node)
    {
        AddHeader("CAJA");
        AddColor("Fondo", node.Background, v => node.Background = v);
        AddColor("Borde", node.BorderColor, v => node.BorderColor = v);
        AddNumber("Ancho borde", node.BorderWidth, v => node.BorderWidth = v);
        AddNumber("Pad arriba", node.Padding.Top, v => node.Padding.Top = v ?? 0);
        AddNumber("Pad derecha", node.Padding.Right, v => node.Padding.Right = v ?? 0);
        AddNumber("Pad abajo", node.Padding.Bottom, v => node.Padding.Bottom = v ?? 0);
        AddNumber("Pad izquierda", node.Padding.Left, v => node.Padding.Left = v ?? 0);
    }

    private void AddTextStyle(LayoutNode node)
    {
        AddHeader("ESTILO");
        AddNumber("Tamaño", node.Style?.FontSize, v => { node.Style ??= new StyleDesign(); node.Style.FontSize = v; });
        AddBool("Negrita", node.Style?.Bold ?? false, v => { node.Style ??= new StyleDesign(); node.Style.Bold = v; });
        AddBool("Cursiva", node.Style?.Italic ?? false, v => { node.Style ??= new StyleDesign(); node.Style.Italic = v; });
        AddColor("Color", node.Style?.Color, v => { node.Style ??= new StyleDesign(); node.Style.Color = v; });
        AddCombo("Alineación", new[] { "left", "center", "right" }, node.Style?.Align ?? "left", v => { node.Style ??= new StyleDesign(); node.Style.Align = v; });
    }

    private void BuildColumnsEditor(LayoutNode table)
    {
        AddHeader("COLUMNAS");
        var list = new ListBox { Width = 300, Height = 90 };
        foreach (var col in table.Columns)
            list.Items.Add($"{col.Header}  ·  {(col.Width is > 0 ? col.Width.Value + "pt" : "rel " + (col.Relative ?? 1))}");
        list.SelectedIndexChanged += (_, _) =>
        {
            var idx = list.SelectedIndex;
            if (idx >= 0 && idx < table.Columns.Count) _editingColumn = table.Columns[idx];
            BuildProperties();
        };
        _propsHost.Controls.Add(list);

        var btns = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
        var add = new Button { Text = "＋ Col" };
        add.Click += (_, _) => { table.Columns.Add(new TableColumn { Header = "Campo", Binding = new BindingDesign(), Relative = 1 }); BuildProperties(); };
        var del = new Button { Text = "－ Col" };
        del.Click += (_, _) => { if (list.SelectedIndex >= 0 && list.SelectedIndex < table.Columns.Count) { table.Columns.RemoveAt(list.SelectedIndex); BuildProperties(); } };
        btns.Controls.Add(add);
        btns.Controls.Add(del);
        _propsHost.Controls.Add(btns);

        if (_editingColumn != null)
        {
            AddHeader("COLUMNA SELECCIONADA");
            AddText("Encabezado", _editingColumn.Header, v => { _editingColumn.Header = v; BuildProperties(); });
            AddText("XPath (rel. fila)", _editingColumn.Binding?.Xpath ?? "", v => { _editingColumn.Binding ??= new BindingDesign(); _editingColumn.Binding.Xpath = v; });
            AddText("Expresión C#", _editingColumn.Binding?.Expr ?? "", v => { _editingColumn.Binding ??= new BindingDesign(); _editingColumn.Binding.Expr = v; });
            AddCombo("Formato", new[] { "text", "currency", "currency2", "number", "number2", "upper", "lower" }, _editingColumn.Format ?? "text", v => { _editingColumn.Format = v; BuildProperties(); });
            AddNumber("Ancho fijo (pt)", _editingColumn.Width, v => { _editingColumn.Width = v; _editingColumn.Relative = null; BuildProperties(); });
            AddNumber("Peso relativo", _editingColumn.Relative, v => { _editingColumn.Relative = v; _editingColumn.Width = null; BuildProperties(); });
        }
    }

    /* ── Helpers de propiedades ────────────────────────────── */

    private void AddHeader(string text)
        => _propsHost.Controls.Add(new Label { Text = text, AutoSize = true, Font = new Font(Font, FontStyle.Bold), ForeColor = Color.FromArgb(0, 130, 120), Margin = new Padding(0, 12, 0, 4) });

    private void AddText(string label, string value, Action<string> apply)
    {
        var tb = new TextBox { Location = new Point(114, 2), Width = 190 };
        tb.Text = value;
        tb.Leave += (_, _) => { apply(tb.Text); RefreshTree(); };
        _propsHost.Controls.Add(Row(label, tb));
    }

    private void AddNumber(string label, float? value, Action<float?> apply)
    {
        var tb = new TextBox { Location = new Point(114, 2), Width = 190 };
        tb.Text = value?.ToString("0.###") ?? "";
        tb.Leave += (_, _) => { apply(float.TryParse(tb.Text, out var v) ? v : null); RefreshTree(); };
        _propsHost.Controls.Add(Row(label, tb));
    }

    private void AddBool(string label, bool value, Action<bool> apply)
    {
        var row = new Panel { Width = 310, Height = 26, Margin = new Padding(0, 1, 0, 1) };
        var cb = new CheckBox { Text = label, AutoSize = true, Location = new Point(0, 3), Checked = value };
        cb.CheckedChanged += (_, _) => { apply(cb.Checked); RefreshTree(); };
        row.Controls.Add(cb);
        _propsHost.Controls.Add(row);
    }

    private void AddCombo(string label, string[] options, string value, Action<string> apply)
    {
        var cb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(114, 2), Width = 190 };
        foreach (var o in options) cb.Items.Add(o);
        cb.SelectedIndex = Array.IndexOf(options, value);
        if (cb.SelectedIndex < 0) cb.SelectedIndex = 0;
        cb.SelectedIndexChanged += (_, _) => { if (cb.SelectedItem is string s) { apply(s); RefreshTree(); } };
        _propsHost.Controls.Add(Row(label, cb));
    }

    private void AddColor(string label, string? value, Action<string?> apply)
    {
        var tb = new TextBox { Location = new Point(114, 2), Width = 190 };
        tb.Text = value ?? "";
        tb.Leave += (_, _) => { apply(string.IsNullOrWhiteSpace(tb.Text) ? null : tb.Text); RefreshTree(); };
        _propsHost.Controls.Add(Row(label, tb));
    }

    private Panel Row(string label, Control input)
    {
        var row = new Panel { Width = 310, Height = 28, Margin = new Padding(0, 1, 0, 1) };
        row.Controls.Add(new Label { Text = label, Width = 110, Location = new Point(0, 6), ForeColor = Color.FromArgb(90, 98, 110) });
        row.Controls.Add(input);
        return row;
    }

    /* ── Preview y render ──────────────────────────────────── */

    private void RenderPreview()
    {
        foreach (Control c in _previewPages.Controls) c.Dispose();
        _previewPages.Controls.Clear();

        if (string.IsNullOrWhiteSpace(_xmlText))
        {
            _previewPages.Controls.Add(new Label { Text = "Carga un XML de muestra (📂 XML) para ver el preview.", AutoSize = true, ForeColor = Color.Gray, Margin = new Padding(20) });
            return;
        }

        try
        {
            QuestPDF.Settings.License = LicenseType.Community;
            var renderer = new DesignDocumentRenderer(_design, CreateResolver()!);
            var images = renderer.GenerateImages(new ImageGenerationSettings { RasterDpi = 110 }).ToList();

            foreach (var bytes in images)
            {
                using var ms = new MemoryStream(bytes);
                using var img = Image.FromStream(ms);
                _previewPages.Controls.Add(new PictureBox
                {
                    Image = new Bitmap(img),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Width = 400,
                    Height = 520,
                    BorderStyle = BorderStyle.FixedSingle,
                    Margin = new Padding(8),
                    BackColor = Color.White
                });
            }
            SetStatus($"Preview: {images.Count} página(s)");
        }
        catch (Exception ex)
        {
            SetStatus("Error en preview: " + ex.Message);
            _previewPages.Controls.Add(new Label { Text = ex.Message, AutoSize = true, ForeColor = Color.FromArgb(192, 57, 43), Margin = new Padding(20), MaximumSize = new Size(500, 0) });
        }
    }

    private void RenderPdf()
    {
        if (string.IsNullOrWhiteSpace(_xmlText)) { SetStatus("No hay XML de muestra."); return; }
        try
        {
            QuestPDF.Settings.License = LicenseType.Community;
            var bytes = new DesignDocumentRenderer(_design, CreateResolver()!).GeneratePdf();
            Directory.CreateDirectory(OutDir);
            var path = Path.Combine(OutDir, $"DESIGN_OUTPUT_{_clientName}.pdf");
            File.WriteAllBytes(path, bytes);
            SetStatus($"PDF generado ({bytes.Length / 1024} KB): {path}");
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            SetStatus("Error al renderizar: " + ex.Message);
            MessageBox.Show(this, ex.Message, "Error al renderizar", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void GenerateCSharp()
    {
        try
        {
            var map = BindingMap.Load(ClientData.BindingMapPath(_clientName));
            var code = DesignToCSharpGenerator.Generate(_design, map);
            Directory.CreateDirectory(GeneratedDir);
            var className = DesignToCSharpGenerator.GenerateClassName(_design);
            var path = Path.Combine(GeneratedDir, className + ".cs");
            File.WriteAllText(path, code);
            SetStatus($"Template C# generado: {path}");
            MessageBox.Show(this, "Template C# generado en:\n" + path, "Codegen", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            SetStatus("Error al generar C#: " + ex.Message);
            MessageBox.Show(this, ex.Message, "Codegen", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    /* ── Archivos ──────────────────────────────────────────── */

    private void SaveDesign()
    {
        try
        {
            var path = _activeDesignPath ?? ClientData.DefaultDesignPath(_clientName);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, DesignerJson.Serialize(_design));
            _activeDesignPath = path;
            ClientData.RememberLastDesign(_clientName, path);
            SetStatus("Diseño guardado: " + Path.GetFileName(path));
        }
        catch (Exception ex) { SetStatus("Error al guardar: " + ex.Message); }
    }

    private void ExportJson()
    {
        using var d = new SaveFileDialog { Filter = "JSON (*.json)|*.json", FileName = "design.json" };
        if (d.ShowDialog(this) != DialogResult.OK) return;
        File.WriteAllText(d.FileName, DesignerJson.Serialize(_design));
        SetStatus("Exportado: " + d.FileName);
    }

    private void ImportJson()
    {
        using var d = new OpenFileDialog { Filter = "Diseño (*.design.json)|*.design.json|JSON (*.json)|*.json", InitialDirectory = _workDir };
        if (d.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            _design = DesignerJson.Load(d.FileName);
            _activeDesignPath = d.FileName;
            RefreshTree();
            BuildProperties();
            RenderPreview();
            SetStatus("Importado: " + d.FileName);
        }
        catch (Exception ex) { SetStatus("Error al importar: " + ex.Message); }
    }

    private void PageSetup()
    {
        using var d = new PageSetupDialog(_design.Page);
        d.ShowDialog(this);
        RenderPreview();
    }

    private void LoadSampleXml()
    {
        using var d = new OpenFileDialog { Filter = "XML (*.xml)|*.xml", Title = "XML de muestra" };
        if (d.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            _xmlText = File.ReadAllText(d.FileName);
            MergeNamespaces(_xmlText);
            RefreshDataSource();
            RenderDataFields();
            RenderPreview();
            SetStatus("XML cargado: " + d.FileName);
        }
        catch (Exception ex)
        {
            SetStatus("Error al cargar XML: " + ex.Message);
        }
    }

    /* ── Undo / Redo ───────────────────────────────────────── */

    private void PushUndo()
    {
        _undo.Push(DesignerJson.Serialize(_design));
        _redo.Clear();
    }

    private void Undo()
    {
        if (_undo.Count == 0) { SetStatus("Nada que deshacer"); return; }
        _redo.Push(DesignerJson.Serialize(_design));
        _design = DesignerJson.Deserialize(_undo.Pop());
        RefreshTree();
        BuildProperties();
    }

    private void Redo()
    {
        if (_redo.Count == 0) { SetStatus("Nada que rehacer"); return; }
        _undo.Push(DesignerJson.Serialize(_design));
        _design = DesignerJson.Deserialize(_redo.Pop());
        RefreshTree();
        BuildProperties();
    }

    private void SetStatus(string text) => _status.Text = text;
}

using PdfQuickDebug.Designer.Model;

namespace PdfQuickDebug.Designer.WinForms;

/// <summary>
/// Configura tamaño, orientación, dimensiones custom y márgenes de la página
/// del diseño. Escribe directamente sobre el PageDesign recibido (vía ApplyTo).
/// </summary>
public sealed class PageSetupDialog : Form
{
    private readonly PageDesign _target;
    private readonly ComboBox _sizeCombo;
    private readonly RadioButton _portrait;
    private readonly RadioButton _landscape;
    private readonly NumericUpDown _width;
    private readonly NumericUpDown _height;
    private readonly NumericUpDown _mTop;
    private readonly NumericUpDown _mRight;
    private readonly NumericUpDown _mBottom;
    private readonly NumericUpDown _mLeft;

    private const string CustomSize = "Custom";
    private const int PtMax = 100000;

    public PageSetupDialog(PageDesign target)
    {
        _target = target;
        Text = "Configurar página";
        Width = 420;
        Height = 470;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 9f);
        MaximizeBox = false;
        MinimizeBox = false;

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(16),
            BackColor = Color.White
        };

        // ── Tamaño ─────────────────────────────────────────────
        var sizeCaption = new Label { Text = "Tamaño de papel:", AutoSize = true, Margin = new Padding(0, 0, 0, 4) };
        flow.Controls.Add(sizeCaption);

        _sizeCombo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 340
        };
        foreach (var s in PageSizeCatalog.Sizes)
            _sizeCombo.Items.Add(PageSizeCatalog.Label(s));
        _sizeCombo.Items.Add(CustomSize);
        flow.Controls.Add(_sizeCombo);

        // ── Orientación ────────────────────────────────────────
        var orientCaption = new Label { Text = "Orientación:", AutoSize = true, Margin = new Padding(0, 12, 0, 4) };
        flow.Controls.Add(orientCaption);

        _portrait = new RadioButton { Text = "Vertical (Portrait)", AutoSize = true, Checked = true };
        _landscape = new RadioButton { Text = "Horizontal (Landscape)", AutoSize = true, Margin = new Padding(24, 0, 0, 0) };
        flow.Controls.Add(_portrait);
        flow.Controls.Add(_landscape);

        // ── Dimensiones custom ─────────────────────────────────
        var dimCaption = new Label { Text = "Dimensiones personalizadas (pt):", AutoSize = true, Margin = new Padding(0, 12, 0, 4) };
        flow.Controls.Add(dimCaption);

        _width = MakeNumber("Ancho", 612f);
        _height = MakeNumber("Alto", 792f);
        flow.Controls.Add(MakeRow("Ancho:", _width, "Alto:", _height));

        // ── Márgenes ───────────────────────────────────────────
        var marginCaption = new Label { Text = "Márgenes (pt):", AutoSize = true, Margin = new Padding(0, 12, 0, 4) };
        flow.Controls.Add(marginCaption);

        _mTop = MakeNumber("Top", 20f);
        _mRight = MakeNumber("Right", 20f);
        _mBottom = MakeNumber("Bottom", 20f);
        _mLeft = MakeNumber("Left", 20f);
        flow.Controls.Add(MakeRow("Superior:", _mTop, "Derecho:", _mRight));
        flow.Controls.Add(MakeRow("Inferior:", _mBottom, "Izquierdo:", _mLeft));

        // ── Botones ────────────────────────────────────────────
        var ok = new Button { Text = "Aceptar", Width = 150, Height = 30, DialogResult = DialogResult.None };
        ok.Click += (_, _) => ApplyAndClose();
        var cancel = new Button { Text = "Cancelar", Width = 150, Height = 30, DialogResult = DialogResult.Cancel };
        cancel.Click += (_, _) => DialogResult = DialogResult.Cancel;
        var btnRow = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            AutoSize = true,
            Margin = new Padding(0, 14, 0, 0)
        };
        btnRow.Controls.Add(ok);
        btnRow.Controls.Add(new Panel { Width = 10, Height = 1 });
        btnRow.Controls.Add(cancel);
        flow.Controls.Add(btnRow);

        Controls.Add(flow);
        AcceptButton = ok;
        CancelButton = cancel;

        _sizeCombo.SelectedIndexChanged += (_, _) => UpdateSizeCustomEnabled();
        LoadFromTarget();
    }

    private void LoadFromTarget()
    {
        var (w, h) = PageSizeCatalog.Resolve(_target);

        // Tamaño: usa _target.Size si es conocido, si no lo infiere de las dimensiones.
        var current = PageSizeCatalog.TryGet(_target.Size) is null
            ? PageSizeCatalog.Detect(w, h)
            : _target.Size;

        var idx = _sizeCombo.Items.IndexOf(current == CustomSize ? CustomSize : PageSizeCatalog.Label(current));
        if (idx < 0)
        {
            _sizeCombo.Items.Add(CustomSize);
            idx = _sizeCombo.Items.Count - 1;
        }
        _sizeCombo.SelectedIndex = idx;

        var orientation = string.Equals(_target.Orientation, "Landscape", StringComparison.OrdinalIgnoreCase);
        _portrait.Checked = !orientation;
        _landscape.Checked = orientation;

        _width.Value = Clamp(w);
        _height.Value = Clamp(h);
        var m = _target.Margins ?? (_target.Margins = new MarginDesign());
        _mTop.Value = Clamp(m.Top);
        _mRight.Value = Clamp(m.Right);
        _mBottom.Value = Clamp(m.Bottom);
        _mLeft.Value = Clamp(m.Left);

        UpdateSizeCustomEnabled();
    }

    private void UpdateSizeCustomEnabled()
    {
        var isCustom = _sizeCombo.SelectedItem as string == CustomSize;
        _width.Enabled = isCustom;
        _height.Enabled = isCustom;
    }

    private void ApplyAndClose()
    {
        var selected = _sizeCombo.SelectedItem as string ?? string.Empty;
        var isCustom = selected == CustomSize;
        var size = isCustom ? CustomSize : selected.Split('·')[0].Trim();

        bool landscape = _landscape.Checked;
        var computed = PageSizeCatalog.TryGet(size);

        if (isCustom)
        {
            _target.Size = CustomSize;
            _target.Width = (float)_width.Value;
            _target.Height = (float)_height.Value;
        }
        else if (computed is { } d)
        {
            _target.Size = size;
            _target.Orientation = landscape ? "Landscape" : "Portrait";
            _target.Width = 0;
            _target.Height = 0;
        }

        var m = _target.Margins ??= new MarginDesign();
        m.Top = (float)_mTop.Value;
        m.Right = (float)_mRight.Value;
        m.Bottom = (float)_mBottom.Value;
        m.Left = (float)_mLeft.Value;

        DialogResult = DialogResult.OK;
    }

    private static NumericUpDown MakeNumber(string name, float initial)
    {
        return new NumericUpDown
        {
            Name = name,
            Minimum = 0,
            Maximum = PtMax,
            DecimalPlaces = 2,
            Increment = 5,
            Width = 130,
            Value = Clamp(initial)
        };
    }

    private static FlowLayoutPanel MakeRow(string label1, NumericUpDown n1, string label2, NumericUpDown n2)
    {
        var row = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            Margin = new Padding(0, 4, 0, 0)
        };
        row.Controls.Add(new Label { Text = label1, AutoSize = true, Margin = new Padding(0, 3, 6, 0), Width = 64 });
        row.Controls.Add(n1);
        row.Controls.Add(new Label { Text = label2, AutoSize = true, Margin = new Padding(14, 3, 6, 0), Width = 58 });
        row.Controls.Add(n2);
        return row;
    }

    private static decimal Clamp(float v) => Math.Clamp((decimal)v, 0, PtMax);
}
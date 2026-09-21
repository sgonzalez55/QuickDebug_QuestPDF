namespace PdfQuickDebug.Designer.WinForms;

/// <summary>Etiqueta flotante que muestra el campo arrastrado junto al cursor.</summary>
internal sealed class DragPreview : Form
{
    private static DragPreview? _current;

    private DragPreview(string text)
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        BackColor = Color.FromArgb(255, 250, 214);

        var label = new Label
        {
            Text = text,
            AutoSize = true,
            BackColor = BackColor,
            ForeColor = Color.FromArgb(40, 40, 40),
            Font = new Font("Consolas", 9f, FontStyle.Bold),
            Padding = new Padding(8, 4, 8, 4),
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(2)
        };
        Controls.Add(label);
    }

    protected override bool ShowWithoutActivation => true;

    public static bool VisibleFlag => _current != null;

    public static void Show(string text, Point screen)
    {
        Hide();
        _current = new DragPreview(text);
        _current.Location = new Point(screen.X + 12, screen.Y + 18);
        _current.Show();
    }

    public static void Move(Point screen)
    {
        if (_current != null)
            _current.Location = new Point(screen.X + 12, screen.Y + 18);
    }

    public static void Hide()
    {
        _current?.Close();
        _current = null;
    }
}
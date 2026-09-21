using System.IO;

namespace PdfQuickDebug.Designer.WinForms;

/// <summary>
/// Lista los clientes existentes para cambiar entre ellos, o permite escribir el
/// nombre de uno nuevo. No crea la carpeta: el llamador decide (para poder pedir
/// confirmación de guardado antes de crear). Devuelve el nombre elegido.
/// </summary>
public sealed class ClientPickerForm : Form
{
    private readonly ListBox _list;
    private readonly string? _current;

    public string? ClientName { get; private set; }

    public ClientPickerForm(List<string> clients, string? current = null, string title = "Cliente")
    {
        _current = current;

        Text = title;
        Width = 380;
        Height = 340;
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
            AutoScroll = true,
            BackColor = Color.White
        };

        flow.Controls.Add(new Label { Text = "Clientes existentes:", AutoSize = true, Margin = new Padding(0, 0, 0, 6) });

        _list = new ListBox { Width = 300, Height = 150, IntegralHeight = false };
        foreach (var c in clients)
            _list.Items.Add(c);
        if (current != null)
        {
            var i = _list.Items.IndexOf(current);
            if (i >= 0) _list.SelectedIndex = i;
        }
        flow.Controls.Add(_list);

        var createBtn = new Button
        {
            Text = "➕  Nuevo cliente…",
            Width = 300,
            Height = 30,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 10, 0, 10),
            Cursor = Cursors.Hand
        };
        createBtn.Click += (_, _) => AskNewName();
        flow.Controls.Add(createBtn);

        var okBtn = new Button { Text = "Aceptar", Width = 140, Height = 28 };
        okBtn.Click += (_, _) => OkSelected();
        var cancelBtn = new Button { Text = "Cancelar", Width = 140, Height = 28, DialogResult = DialogResult.Cancel };
        flow.Controls.Add(okBtn);
        flow.Controls.Add(cancelBtn);

        Controls.Add(flow);
    }

    private void OkSelected()
    {
        var sel = _list.SelectedItem as string;
        if (string.IsNullOrWhiteSpace(sel))
        {
            MessageBox.Show(this, "Seleccioná un cliente de la lista o creá uno nuevo.",
                "Cliente", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        ClientName = sel;
        DialogResult = DialogResult.OK;
    }

    private void AskNewName()
    {
        var name = PromptNewName(this);
        if (string.IsNullOrWhiteSpace(name)) return;

        ClientName = name;
        DialogResult = DialogResult.OK;
    }

    /// <summary>Pide el nombre de un cliente nuevo (valida que no exista). Devuelve null si se cancela.</summary>
    public static string? PromptNewName(IWin32Window owner)
    {
        using var form = new Form
        {
            Text = "Nuevo cliente",
            Width = 380,
            Height = 170,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false,
            MinimizeBox = false,
            Font = new Font("Segoe UI", 9f)
        };

        var caption = new Label
        {
            Text = "Nombre del cliente (será la carpeta del pipeline):",
            Location = new Point(16, 14),
            AutoSize = true
        };
        var box = new TextBox { Location = new Point(16, 38), Width = 320 };

        var ok = new Button { Text = "Crear", DialogResult = DialogResult.OK, Location = new Point(16, 76), Width = 120 };
        var cancel = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Location = new Point(148, 76), Width = 120 };

        form.Controls.Add(caption);
        form.Controls.Add(box);
        form.Controls.Add(ok);
        form.Controls.Add(cancel);
        form.AcceptButton = ok;
        form.CancelButton = cancel;

        if (form.ShowDialog(owner) != DialogResult.OK) return null;

        var name = box.Text.Trim();
        if (string.IsNullOrWhiteSpace(name)) return null;

        if (ClientData.Exists(name))
        {
            MessageBox.Show(owner, "Ya existe un cliente con ese nombre.\nSeleccionalo en la lista.",
                "Nuevo cliente", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return null;
        }

        return name;
    }
}
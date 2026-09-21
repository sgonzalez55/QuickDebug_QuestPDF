using System.IO;

namespace PdfQuickDebug.Designer.WinForms;

public enum StartAction
{
    Nuevo,
    Cargar
}

/// <summary>
/// Pantalla inicial de la app: elegís el cliente (creándolo si hace falta) y luego
/// un diseño nuevo (el nombre del archivo define la clase/static() del template)
/// o cargás un diseño existente del cliente.
/// </summary>
public sealed class StartForm : Form
{
    private readonly ComboBox _clientBox;

    public StartAction? Mode { get; private set; }
    public string? ChosenPath { get; private set; }
    public string ClientName => _clientBox.SelectedItem as string ?? string.Empty;

    /// <summary>XML elegido al crear un cliente nuevo: el diseñador corre el pipeline y genera DTO.</summary>
    public string? OnboardXmlPath { get; private set; }

    public StartForm()
    {
        var clients = ClientData.ListClients();
        if (clients.Count == 0)
        {
            ClientData.CreateClient("AJECOLOMBIA");
            clients = ClientData.ListClients();
        }

        Text = "Inicio — Diseñador QuestPDF";
        Width = 460;
        Height = 460;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9f);
        BackColor = Color.White;

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(24, 16, 24, 16),
            BackColor = Color.White
        };

        var title = new Label
        {
            Text = "Diseñador QuestPDF",
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            ForeColor = Color.FromArgb(31, 35, 40),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 2)
        };
        var subtitle = new Label
        {
            Text = "¿Qué querés hacer?",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = Color.FromArgb(120, 128, 138),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 12)
        };

        var clientRow = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = new Padding(0, 0, 0, 16)
        };
        var clientLbl = new Label
        {
            Text = "Cliente (carpeta con su propio XSLT y datos):",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(100, 106, 116),
            AutoSize = true
        };
        _clientBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 340,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
        };
        foreach (var c in clients)
            _clientBox.Items.Add(c);
        if (clients.Count > 0) _clientBox.SelectedIndex = 0;

        var newClient = new Button
        {
            Text = "➕  Nuevo cliente",
            FlatStyle = FlatStyle.Flat,
            Width = 340,
            Height = 30,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 2, 0, 0)
        };
        newClient.Click += (_, _) => CreateNewClient();

        clientRow.Controls.Add(clientLbl);
        clientRow.Controls.Add(_clientBox);
        clientRow.Controls.Add(newClient);

        flow.Controls.Add(title);
        flow.Controls.Add(subtitle);
        flow.Controls.Add(clientRow);
        flow.Controls.Add(Card("➕  Nuevo", "Crear un template desde cero para este cliente. El nombre del archivo define la clase (ej: FacturaUbl.cs → public class FacturaUbl).", PickNew));
        flow.Controls.Add(Card("📂  Cargar", "Abrir un diseño guardado de este cliente (.design.json) para editarlo.", PickLoad));

        Controls.Add(flow);
    }

    private void CreateNewClient()
    {
        var name = ClientPickerForm.PromptNewName(this);
        if (string.IsNullOrWhiteSpace(name)) return;

        using var xmlDialog = new OpenFileDialog
        {
            Filter = "XML (*.xml)|*.xml",
            Title = "XML del cliente nuevo (se transformará con el XSLT de referencia DIAN)"
        };
        if (xmlDialog.ShowDialog(this) != DialogResult.OK) return;

        ClientData.CreateClient(name);
        OnboardXmlPath = xmlDialog.FileName;
        if (!_clientBox.Items.Contains(name)) _clientBox.Items.Add(name);
        _clientBox.SelectedItem = name;

        // Entra directo al diseñador con un diseño base del cliente nuevo; allí se
        // corre el pipeline (transformado → contrato → DTO) con el XML elegido.
        Mode = StartAction.Nuevo;
        ChosenPath = ClientData.DefaultDesignPath(name);
        DialogResult = DialogResult.OK;
    }

    private string? ClientDirOrNull()
    {
        if (string.IsNullOrWhiteSpace(ClientName)) return null;
        var dir = ClientData.WorkDir(ClientName);
        return Directory.Exists(dir) ? dir : null;
    }

    private Control Card(string title, string caption, Action onClick)
    {
        var card = new Panel
        {
            Size = new Size(380, 92),
            BackColor = Color.FromArgb(245, 247, 251),
            Margin = new Padding(0, 0, 0, 12),
            Cursor = Cursors.Hand
        };
        card.Padding = new Padding(12);

        var t = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = Color.FromArgb(0, 90, 150),
            AutoSize = true,
            Location = new Point(12, 10)
        };
        var c = new Label
        {
            Text = caption,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(100, 106, 116),
            AutoSize = true,
            MaximumSize = new Size(352, 0),
            Location = new Point(12, 36)
        };

        card.Controls.Add(t);
        card.Controls.Add(c);
        card.Click += (_, _) => onClick();
        foreach (Control child in card.Controls)
            child.Click += (_, _) => onClick();
        return card;
    }

    private void PickNew()
    {
        var clientDir = ClientDirOrNull();
        if (clientDir == null)
        {
            MessageBox.Show(this, "Elegí o creá un cliente antes.", "Cliente",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Title = "Crear nuevo diseño",
            Filter = "Diseño (*.design.json)|*.design.json|JSON (*.json)|*.json",
            FileName = ClientName + ".design.json",
            InitialDirectory = clientDir
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        Mode = StartAction.Nuevo;
        ChosenPath = dialog.FileName;
        DialogResult = DialogResult.OK;
    }

    private void PickLoad()
    {
        var clientDir = ClientDirOrNull();
        if (clientDir == null)
        {
            MessageBox.Show(this, "Elegí o creá un cliente antes.", "Cliente",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new OpenFileDialog
        {
            Title = "Cargar diseño",
            Filter = "Diseño (*.design.json)|*.design.json|JSON (*.json)|*.json",
            InitialDirectory = clientDir
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        Mode = StartAction.Cargar;
        ChosenPath = dialog.FileName;
        DialogResult = DialogResult.OK;
    }
}
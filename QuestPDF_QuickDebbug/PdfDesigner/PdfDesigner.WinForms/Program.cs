namespace PdfQuickDebug.Designer.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => Report(e.Exception, "ThreadException");
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Report(e.ExceptionObject as Exception, "UnhandledException");

        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

        var fontPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", "LiberationSans-Regular.ttf");
        if (File.Exists(fontPath))
            QuestPDF.Drawing.FontManager.RegisterFont(File.OpenRead(fontPath));

        // Migrar los datos heredados de data\ al cliente AJECOLOMBIA la primera vez.
        ClientData.EnsureMigration();

        using (var start = new StartForm())
        {
            if (start.ShowDialog() != DialogResult.OK) return;
            if (start.Mode == null || string.IsNullOrWhiteSpace(start.ChosenPath)) return;
            if (string.IsNullOrWhiteSpace(start.ClientName)) return;
            Application.Run(new DesignerForm(start.ClientName, start.Mode.Value, start.ChosenPath, start.OnboardXmlPath));
        }
    }

    private static void Report(Exception? ex, string kind)
    {
        if (ex == null) return;

        try
        {
            var dir = Path.Combine(ClientData.RootDir, "output");
            Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "designer-error.log"),
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {kind}: {ex}\r\n\r\n");
        }
        catch
        {
            // el log es solo diagnóstico
        }

        MessageBox.Show(ex.Message, "Error del diseñador", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}

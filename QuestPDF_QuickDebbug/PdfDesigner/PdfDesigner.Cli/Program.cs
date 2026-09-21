using PdfQuickDebug.Core;
using PdfQuickDebug.Designer;
using PdfQuickDebug.Designer.CodeGen;
using PdfQuickDebug.Designer.Data;
using PdfQuickDebug.Designer.Model;

namespace PdfDesigner.Cli;

/// <summary>
/// Consola del diseñador PdfDesigner.
/// Uso: PdfDesigner.Cli.exe [modo]
///   --save-transformed   Guarda el XML transformado por el XSLT (muestra real del pipeline)
///   --resolve-test       Comprueba cuántos campos del contrato resuelven contra la muestra
///   --contract           (Re)genera data-contract.json desde binding-map.json + pipeline
///   --design             Renderiza factura.design.json a PDF
///   --designer-ui | --ui  Arranca el editor web drag & drop en http://localhost:12500
/// </summary>
internal static class Program
{
    // ─────────────────────────────────────────────────────────────────────
    // RUTAS (todo vive dentro de la carpeta única PdfDesigner)
    // ─────────────────────────────────────────────────────────────────────
    private const string Root =
        @"C:\Users\santiago.gonzalez\Downloads\QuestPDF_QuickDebbug_v3\QuestPDF_QuickDebbug\PdfDesigner";

    private const string DataDir = Root + @"\data";
    private const string OutDir = Root + @"\output";

    private const string PipelineXmlPath = DataDir + @"\NUEVO_XML.xml";
    private const string PipelineXsltPath = DataDir + @"\FACTURA-UBL.xslt";
    private const string SampleXml = DataDir + @"\factura.sample.xml";
    private const string TransformedXml = DataDir + @"\factura.transformed.xml";
    private const string DesignJson = DataDir + @"\factura.design.json";
    private const string DesignOutput = OutDir + @"\DESIGN_OUTPUT.pdf";
    private const string BindingMapPath = DataDir + @"\binding-map.json";
    private const string DataContractPath = DataDir + @"\data-contract.json";

    [STAThread]
    private static void Main(string[] args)
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

        var fontPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts", "LiberationSans-Regular.ttf");
        if (File.Exists(fontPath))
            QuestPDF.Drawing.FontManager.RegisterFont(File.OpenRead(fontPath));

        // 🧾 Guardar el XML transformado por el XSLT (muestra real del pipeline)
        //    PdfDesigner.Cli --save-transformed
        if (args.Contains("--save-transformed"))
        {
            var rawXml = File.ReadAllText(PipelineXmlPath);
            var xslt = File.ReadAllText(PipelineXsltPath);
            var transformed = new XsltTransform().Transform(rawXml, xslt);
            // El XSLT produce un fragmento multiraíz: se envuelve igual que hace XmlParser.
            File.WriteAllText(TransformedXml, "<Root>" + transformed + "</Root>");
            Console.WriteLine($"XML transformado guardado ({transformed.Length} chars): {TransformedXml}");
            return;
        }

        // 🧪 Probar resolución de XPaths contra el XML transformado (diagnóstico)
        //    PdfDesigner.Cli --resolve-test
        if (args.Contains("--resolve-test"))
        {
            var resolver = new XmlDataResolver(File.ReadAllText(TransformedXml), null);
            var contract = DataContract.Load(DataContractPath);
            var model = contract?.Fields.Where(f => f.Group == "Modelo").ToList() ?? new List<DataContract.Field>();
            var custom = contract?.Fields.Where(f => f.Group == "Custom").ToList() ?? new List<DataContract.Field>();
            var okModel = model.Count(f => !string.IsNullOrWhiteSpace(resolver.ResolveString(f.Xpath)));
            var okCustom = custom.Count(f => !string.IsNullOrWhiteSpace(resolver.ResolveString(f.Xpath)));
            Console.WriteLine($"Modelo: {okModel}/{model.Count} resuelven");
            Console.WriteLine($"Custom: {okCustom}/{custom.Count} resuelven");
            foreach (var f in model.Take(6))
                Console.WriteLine($"  {f.Name,-22} {f.Xpath,-42} => '{resolver.ResolveString(f.Xpath)}'");
            foreach (var f in custom.Take(3))
                Console.WriteLine($"  {f.Name,-30} => '{resolver.ResolveString(f.Xpath)}'");
            Console.WriteLine($"  Detalle(1) descripcion => '{resolver.ResolveString(@"//CFD/Detalle/@descripcion_tx")}'");
            return;
        }

        // 🧾 Descubrir contrato de datos: PdfDesigner.Cli --contract
        if (args.Contains("--contract"))
        {
            var map = BindingMap.Load(BindingMapPath);
            var contract = DataContract.Build(map, PipelineXmlPath, PipelineXsltPath);
            contract.Save(DataContractPath);
            Console.WriteLine($"Contrato generado: {DataContractPath} ({contract.Fields.Count} campos)");
            return;
        }

        // 🎨 Diseñador visual → QuestPDF (POC): PdfDesigner.Cli --design
        if (args.Contains("--design"))
        {
            DesignerRunner.Run(SampleXml, DesignJson, DesignOutput);
            return;
        }

        // ✅ Validar la generación (.cs local y DTO) contra el proyecto PdfQuickDebug:
        //    PdfDesigner.Cli --validate
        if (args.Contains("--validate"))
        {
            ValidateGeneration.Run(Root);
            return;
        }

        // 🧪 Generar variante local (namespace PdfQuickDebug.Templates) para probar en PdfQuickDebug:
        //    PdfDesigner.Cli --cs-local <ruta\design.json>  [--binding <ruta\binding-map.json>]
        var csLocalIdx = Array.IndexOf(args, "--cs-local");
        if (csLocalIdx >= 0 && args.Length > csLocalIdx + 1)
        {
            var designPath = Path.GetFullPath(args[csLocalIdx + 1]);
            var bindingIdx = Array.IndexOf(args, "--binding");
            var bindingPath = bindingIdx >= 0 && args.Length > bindingIdx + 1
                ? Path.GetFullPath(args[bindingIdx + 1])
                : null;

            if (!File.Exists(designPath))
            {
                Console.WriteLine($"ERROR: diseño no encontrado: {designPath}");
                return;
            }

            var design = DesignerJson.Load(designPath);
            var map = !string.IsNullOrWhiteSpace(bindingPath) && File.Exists(bindingPath)
                ? BindingMap.Load(bindingPath)
                : BindingMap.CreateDefault();
            var localDebugDir = Path.Combine(OutDir, "LocalDebug");
            Directory.CreateDirectory(localDebugDir);
            foreach (var stale in Directory.GetFiles(localDebugDir, "*.cs"))
                File.Delete(stale);

            // 1) Template local: clase FacturaUbl (igual que instancia Program.cs:124).
            var templateCode = DesignToCSharpGenerator.Generate(design, map, "FacturaUbl");
            var templatePath = Path.Combine(localDebugDir, "FacturaUbl.cs");
            File.WriteAllText(templatePath, templateCode);
            Console.WriteLine($"Template local generado: {templatePath}");
            Console.WriteLine($"  Clase: FacturaUbl  Namespace: PdfQuickDebug.Templates");

            // 2) DTO local: namespace PdfQuickDebug.Templates (igual que Templates\DtoUbl.cs).
            //    El diseño vive en data\work\<Cliente>\<Cliente>.design.json y el DTO baseline en data\clients\<Cliente>\dto\DtoUbl.cs.
            var designDir = Path.GetDirectoryName(designPath) ?? string.Empty;
            var clientWorkName = new DirectoryInfo(designDir).Name;
            var contractPath = Path.Combine(designDir, "data-contract.json");
            var existingDto = Path.GetFullPath(Path.Combine(designDir, "..", "..", "clients", clientWorkName, "dto", "DtoUbl.cs"));
            if (File.Exists(contractPath))
            {
                var contract = DataContract.Load(contractPath);
                if (contract != null)
                {
                    var dtoCode = DtoToCSharpGenerator.GenerateLocal(contract, existingDto);
                    var dtoPath = Path.Combine(localDebugDir, "DtoUbl.cs");
                    File.WriteAllText(dtoPath, dtoCode);
                    Console.WriteLine($"DTO local generado: {dtoPath}");
                }
            }

            // 3) XSLT con el nombre que usa Program.cs (DataTest\FACTURA-UBL.xslt).
            var clientXslt = Path.GetFullPath(Path.Combine(designDir, "..", "..", "clients", clientWorkName, "ubl.xslt"));
            if (File.Exists(clientXslt))
            {
                File.Copy(clientXslt, Path.Combine(localDebugDir, "FACTURA-UBL.xslt"), overwrite: true);
                Console.WriteLine($"XSLT local generado: FACTURA-UBL.xslt (desde {clientXslt})");
            }
            else
            {
                Console.WriteLine($"  (sin XSLT del cliente: {clientXslt})");
            }

            Console.WriteLine("Copia los archivos a DebugPDF\\PdfQuickDebug\\ (Templates\\ y DataTest\\) para probarlo.");
            return;
        }

        Console.WriteLine("PdfDesigner.Cli — modos disponibles:");
        Console.WriteLine("  --save-transformed   Guarda el XML transformado por el XSLT");
        Console.WriteLine("  --resolve-test       Comprueba campos del contrato vs. muestra");
        Console.WriteLine("  --contract           Genera data-contract.json");
        Console.WriteLine("  --design             Renderiza factura.design.json a PDF");
        Console.WriteLine("  --validate           Regenera DTOs y compila los templates generados");
        Console.WriteLine("  --designer-ui        Arranca el editor web drag & drop");
        Console.WriteLine("  --cs-local <json>    Genera variante local (PdfQuickDebug.Templates) para debug");
    }
}